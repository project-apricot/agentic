using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Offers the tools of the MCP servers this caller reaches.
/// </summary>
/// <remarks>
/// Queried per listing, since servers' tools change at runtime and the servers reached depend on
/// the caller. Foreign <c>McpClientTool</c> instances are used unwrapped.
/// </remarks>
public sealed class McpAgentToolSource : IAgentToolSource, IMcpToolInvalidation
{
    /// <summary>
    /// The client provider.
    /// </summary>
    private readonly IMcpClientProvider clients;

    /// <summary>
    /// The source options.
    /// </summary>
    private readonly IOptionsMonitor<McpAgentToolSourceOptions> options;

    /// <summary>
    /// The tool validators.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolValidator> validators;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<McpAgentToolSource> logger;

    /// <summary>
    /// Cached listings, per server.
    /// </summary>
    private readonly ConcurrentDictionary<McpClient, Listing> listings = new();

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="clients">The client provider.</param>
    /// <param name="options">The source options.</param>
    /// <param name="validators">The tool validators.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public McpAgentToolSource(
        IMcpClientProvider clients,
        IOptionsMonitor<McpAgentToolSourceOptions> options,
        IEnumerable<IAgentToolValidator> validators,
        ILogger<McpAgentToolSource> logger)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validators);
        ArgumentNullException.ThrowIfNull(logger);

        this.clients = clients;
        this.options = options;
        this.validators = [.. validators];
        this.logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the cached listings; does not skip by prefix, since curation can rename tools.
    /// </remarks>
    public async ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        var tools = await this.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        return tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = this.options.CurrentValue;

        var reached = await this.clients.GetClientsAsync(context, cancellationToken).ConfigureAwait(false);

        var offered = new List<AgentToolDescriptor>();

        foreach (var client in reached)
        {
            offered.AddRange(await this.ReadAsync(client, settings, cancellationToken).ConfigureAwait(false));
        }

        return offered;
    }

    /// <inheritdoc />
    public void Invalidate(McpClient? client = null)
    {
        if (client is null)
        {
            this.listings.Clear();
        }
        else
        {
            this.listings.TryRemove(client, out _);
        }
    }

    /// <summary>
    /// Reads a server's tools, using the cache while fresh.
    /// </summary>
    /// <param name="client">The server.</param>
    /// <param name="settings">The source options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    private async ValueTask<IReadOnlyList<AgentToolDescriptor>> ReadAsync(McpClient client, McpAgentToolSourceOptions settings, CancellationToken cancellationToken)
    {
        if (this.listings.TryGetValue(client, out var held) && !held.IsStale(settings.Lifetime))
        {
            return held.Tools;
        }

        var listed = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var offered = new List<AgentToolDescriptor>(listed.Count);

        foreach (var tool in listed)
        {
            if (this.Offer(tool, client, settings) is { } descriptor)
            {
                offered.Add(descriptor);
            }
        }

        this.listings[client] = new Listing(offered, DateTimeOffset.UtcNow);

        return offered;
    }

    /// <summary>
    /// Converts a foreign tool to a descriptor.
    /// </summary>
    /// <param name="tool">The foreign tool.</param>
    /// <param name="client">The server.</param>
    /// <param name="settings">The source options.</param>
    /// <returns>The descriptor, or null to leave it out.</returns>
    private AgentToolDescriptor? Offer(McpClientTool tool, McpClient client, McpAgentToolSourceOptions settings)
    {
        var origin = Origin(client);

        var annotations = tool.ProtocolTool.Annotations;

        var declaration = new AgentToolDeclaration
        {
            Name = Name(tool.Name, origin, settings.Prefix),
            Description = tool.Description,
            IsReadOnly = annotations?.ReadOnlyHint ?? false,
            IsDestructive = annotations?.DestructiveHint ?? settings.DestructiveWhenUnstated,

            // a server this host reaches over a wire is by definition outside the application
            IsOpenWorld = annotations?.OpenWorldHint ?? true,
            Labels = new Dictionary<string, object?>(StringComparer.Ordinal) { [OriginLabel] = origin }
        };

        if (annotations?.IdempotentHint is { } idempotent)
        {
            declaration = declaration with { IsIdempotent = idempotent };
        }

        if (!string.IsNullOrWhiteSpace(tool.Title))
        {
            declaration = declaration with { Title = tool.Title };
        }

        var descriptor = new AgentToolDescriptor(tool, declaration, settings.Metadata);

        if (settings.Curate is { } curate)
        {
            // null leaves it out, which is how an allowlist is written
            if (curate(descriptor) is not { } curated)
            {
                return null;
            }

            descriptor = curated;
        }

        return settings.DropRejected ? this.Accepted(descriptor, origin) : descriptor;
    }

    /// <summary>
    /// Drops and logs a tool the validators refuse.
    /// </summary>
    /// <param name="descriptor">The tool.</param>
    /// <param name="origin">The originating server.</param>
    /// <returns>The tool, or null if dropped.</returns>
    private AgentToolDescriptor? Accepted(AgentToolDescriptor descriptor, string origin)
    {
        foreach (var validator in this.validators)
        {
            try
            {
                validator.Validate(descriptor);
            }
            catch (AgentToolDeclarationException exception)
            {
                McpAgentToolLog.Rejected(this.logger, exception, descriptor.Name, origin);

                return null;
            }
        }

        return descriptor;
    }

    /// <summary>
    /// The label naming a foreign tool's originating server.
    /// </summary>
    public const string OriginLabel = "mcp.origin";

    /// <summary>
    /// Gets a server's name.
    /// </summary>
    /// <param name="client">The server.</param>
    /// <returns>The name.</returns>
    private static string Origin(McpClient client) =>
        client.ServerInfo.Name is { Length: > 0 } name ? name : "mcp";

    /// <summary>
    /// Builds a foreign tool's local name.
    /// </summary>
    /// <param name="name">The server's name for the tool.</param>
    /// <param name="origin">The originating server.</param>
    /// <param name="prefix">The prefix, or null.</param>
    /// <returns>The name.</returns>
    private static string Name(string name, string origin, string? prefix) =>
        prefix is null ? name : $"{prefix}{Slug(origin)}_{name}";

    /// <summary>
    /// Converts a server name to a tool-name segment.
    /// </summary>
    /// <param name="origin">The server's name.</param>
    /// <returns>The segment.</returns>
    /// <remarks>
    /// Uses only the last path segment.
    /// </remarks>
    private static string Slug(string origin)
    {
        var last = origin.AsSpan()[(origin.LastIndexOfAny(['/', ':', '\\']) + 1)..];

        var slug = string.Concat(last.ToString().Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_'));

        return slug.Trim('_') is { Length: > 0 } trimmed ? trimmed : "mcp";
    }

    /// <summary>
    /// A server's cached listing.
    /// </summary>
    /// <param name="Tools">The tools.</param>
    /// <param name="Read">When it was fetched.</param>
    private sealed record Listing(IReadOnlyList<AgentToolDescriptor> Tools, DateTimeOffset Read)
    {
        /// <summary>
        /// Checks whether the listing has expired.
        /// </summary>
        /// <param name="lifetime">The cache lifetime.</param>
        /// <returns>True if it should be refetched.</returns>
        internal bool IsStale(TimeSpan lifetime) => DateTimeOffset.UtcNow - this.Read > lifetime;
    }
}
