using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// The tools whatever MCP servers this caller reaches are offering, now.
/// </summary>
/// <remarks>
/// <para>
/// Asked on every listing rather than once at start-up, which is the whole reason a source takes
/// a caller: what a server offers changes while the process runs, and which servers a person has
/// connected is a property of the person.
/// </para>
/// <para>
/// Nothing wraps a foreign tool. An <c>McpClientTool</c> is already an
/// <c>AIFunction</c>, so it goes into the descriptor as it arrived and what this host claims
/// about it - a name in a space of its own, a description it pinned, a gate - sits beside it.
/// </para>
/// </remarks>
public sealed class McpAgentToolSource : IAgentToolSource, IMcpToolInvalidation
{
    /// <summary>
    /// Which servers a caller reaches.
    /// </summary>
    private readonly IMcpClientProvider clients;

    /// <summary>
    /// How foreign tools are offered here.
    /// </summary>
    private readonly IOptionsMonitor<McpAgentToolSourceOptions> options;

    /// <summary>
    /// What this host would refuse.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolValidator> validators;

    /// <summary>
    /// Where a dropped tool is reported.
    /// </summary>
    private readonly ILogger<McpAgentToolSource> logger;

    /// <summary>
    /// What each server last said it offers.
    /// </summary>
    private readonly ConcurrentDictionary<McpClient, Listing> listings = new();

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="clients">Which servers a caller reaches.</param>
    /// <param name="options">How foreign tools are offered here.</param>
    /// <param name="validators">What this host would refuse.</param>
    /// <param name="logger">Where a dropped tool is reported.</param>
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
    /// Reads one server's tools, from the last answer where it is still fresh.
    /// </summary>
    /// <param name="client">The server.</param>
    /// <param name="settings">How foreign tools are offered here.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
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
    /// Decides how one foreign tool is offered here, or that it is not.
    /// </summary>
    /// <param name="tool">The tool the server offered.</param>
    /// <param name="client">The server.</param>
    /// <param name="settings">How foreign tools are offered here.</param>
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
    /// Leaves out a tool this host would refuse, and says so.
    /// </summary>
    /// <param name="descriptor">The tool.</param>
    /// <param name="origin">Which server it came from.</param>
    /// <returns>The tool, or null where it was left out.</returns>
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
    /// The label every foreign tool carries, saying which server it came from.
    /// </summary>
    public const string OriginLabel = "mcp.origin";

    /// <summary>
    /// Names a server, for a label and for a name space.
    /// </summary>
    /// <param name="client">The server.</param>
    /// <returns>Its name.</returns>
    private static string Origin(McpClient client) =>
        client.ServerInfo?.Name is { Length: > 0 } name ? name : "mcp";

    /// <summary>
    /// Decides what a foreign tool is called here.
    /// </summary>
    /// <param name="name">What the server calls it.</param>
    /// <param name="origin">Which server it came from.</param>
    /// <param name="prefix">What goes in front, or null for nothing.</param>
    /// <returns>The name.</returns>
    private static string Name(string name, string origin, string? prefix) =>
        prefix is null ? name : $"{prefix}{Slug(origin)}_{name}";

    /// <summary>
    /// Renders a server's name so it can be part of a tool's.
    /// </summary>
    /// <param name="origin">The server's name.</param>
    /// <returns>Something a tool name can contain.</returns>
    /// <remarks>
    /// Only the last segment. A server that calls itself <c>mcp-servers/everything</c> is saying
    /// where it came from as much as what it is, and carrying all of that into every tool name
    /// spends a prompt's attention on packaging.
    /// </remarks>
    private static string Slug(string origin)
    {
        var last = origin.AsSpan()[(origin.LastIndexOfAny(['/', ':', '\\']) + 1)..];

        var slug = string.Concat(last.ToString().Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_'));

        return slug.Trim('_') is { Length: > 0 } trimmed ? trimmed : "mcp";
    }

    /// <summary>
    /// What one server last said it offers.
    /// </summary>
    /// <param name="Tools">The tools.</param>
    /// <param name="Read">When it said so.</param>
    private sealed record Listing(IReadOnlyList<AgentToolDescriptor> Tools, DateTimeOffset Read)
    {
        /// <summary>
        /// Whether this is too old to use.
        /// </summary>
        /// <param name="lifetime">How long one is held.</param>
        /// <returns>True where the server should be asked again.</returns>
        internal bool IsStale(TimeSpan lifetime) => DateTimeOffset.UtcNow - this.Read > lifetime;
    }
}
