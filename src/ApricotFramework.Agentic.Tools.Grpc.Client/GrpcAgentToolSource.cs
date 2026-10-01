using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// Offers another service's tools to this caller over gRPC.
/// </summary>
/// <remarks>
/// One per federated service; addressing, deadlines, retries and credentials live on the client
/// registration. No client is held: one is made per listing or call from the caller's scope, since
/// per-client configuration reads services from that scope.
/// </remarks>
public sealed class GrpcAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// Gets a client from the call's services.
    /// </summary>
    private readonly Func<IServiceProvider, AgentTools.AgentToolsClient> client;

    /// <summary>
    /// This source's options.
    /// </summary>
    /// <remarks>
    /// Held per source so each federated service keeps its own prefix and allowlist.
    /// </remarks>
    private readonly GrpcAgentToolSourceOptions options;

    /// <summary>
    /// Cached listings, per caller.
    /// </summary>
    private readonly ConcurrentDictionary<string, Listing> listings = new(StringComparer.Ordinal);

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="client">Gets a client from the call's services.</param>
    /// <param name="options">This source's options.</param>
    /// <param name="logger">The logger, or null.</param>
    private GrpcAgentToolSource(Func<IServiceProvider, AgentTools.AgentToolsClient> client, GrpcAgentToolSourceOptions options, ILogger? logger)
    {
        this.client = client;
        this.options = options;
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Creates a source using a host-defined client type.
    /// </summary>
    /// <typeparam name="TClient">A type derived from the generated client, one per service, registered with <c>AddGrpcClient()</c>.</typeparam>
    /// <param name="options">This source's options.</param>
    /// <param name="logger">The logger, or null.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <remarks>
    /// The client factory ignores namespaces, so two such types need different names.
    /// </remarks>
    public static GrpcAgentToolSource ForClient<TClient>(GrpcAgentToolSourceOptions options, ILogger<GrpcAgentToolSource>? logger = null) where TClient : AgentTools.AgentToolsClient
    {
        ArgumentNullException.ThrowIfNull(options);

        return new GrpcAgentToolSource(Typed<TClient>, options, logger);
    }

    /// <summary>
    /// Creates a source using a named client.
    /// </summary>
    /// <param name="clientName">The name registered with <c>AddGrpcClient&lt;AgentTools.AgentToolsClient&gt;(name)</c>.</param>
    /// <param name="options">This source's options.</param>
    /// <param name="logger">The logger, or null.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is blank.</exception>
    public static GrpcAgentToolSource ForClient(string clientName, GrpcAgentToolSourceOptions options, ILogger<GrpcAgentToolSource>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(options);

        return new GrpcAgentToolSource(services => Named(services, clientName), options, logger);
    }

    /// <summary>
    /// Gets a client of a host-defined type.
    /// </summary>
    /// <typeparam name="TClient">The client type.</typeparam>
    /// <param name="services">The call's services.</param>
    /// <returns>The client.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type was never registered as a client.</exception>
    private static AgentTools.AgentToolsClient Typed<TClient>(IServiceProvider services) where TClient : AgentTools.AgentToolsClient =>
        services.GetService<TClient>() ?? throw new Exceptions.AgentToolConfigurationException($"No gRPC client of type '{typeof(TClient).Name}' is registered, so its agent tools cannot be reached. Register it with AddGrpcClient<{typeof(TClient).Name}>().");

    /// <summary>
    /// Gets a named client.
    /// </summary>
    /// <param name="services">The call's services.</param>
    /// <param name="clientName">The client name.</param>
    /// <returns>The client.</returns>
    /// <exception cref="Exceptions.AgentToolConfigurationException">Thrown when no client is registered under the name.</exception>
    /// <remarks>
    /// Marked as a configuration error so it is not mistaken for the service being down.
    /// </remarks>
    private static AgentTools.AgentToolsClient Named(IServiceProvider services, string clientName)
    {
        try
        {
            return services.GetRequiredService<GrpcClientFactory>().CreateClient<AgentTools.AgentToolsClient>(clientName);
        }
        catch (InvalidOperationException exception) when (exception is not Exceptions.AgentToolConfigurationException)
        {
            throw new Exceptions.AgentToolConfigurationException(exception.Message, exception);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// External unless <see cref="GrpcAgentToolSourceOptions.RequireService"/> is set, so failures cost only this service's tools.
    /// </remarks>
    public bool IsExternal => !this.options.RequireService;

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return (await this.ListAsync(context, cancellationToken).ConfigureAwait(false)).Tools;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Names outside the prefix are skipped without calling the service. Otherwise, uses the caller's
    /// cached listing or one fresh listing, since the contract cannot fetch a single tool.
    /// </remarks>
    public async ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(context);

        if (this.options.Prefix is { } prefix && !name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var listing = await this.ListAsync(context, cancellationToken).ConfigureAwait(false);

        return listing.Index.GetValueOrDefault(name);
    }

    /// <summary>
    /// Gets the listing for this caller, cached where allowed.
    /// </summary>
    /// <param name="context">The caller.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The listing.</returns>
    private async ValueTask<Listing> ListAsync(IAgentToolSourceContext context, CancellationToken cancellationToken)
    {
        var settings = this.options;

        var key = settings.Lifetime > TimeSpan.Zero ? CacheKey(settings, context) : null;

        if (key is not null
            && this.listings.TryGetValue(key, out var held)
            && DateTimeOffset.UtcNow - held.Read <= settings.Lifetime)
        {
            return held;
        }

        var response = await this.client(context.Services).ListToolsAsync(new ListToolsRequest(), cancellationToken: cancellationToken).ConfigureAwait(false);

        var offered = new List<AgentToolDescriptor>(response.Tools.Count);
        var index = new Dictionary<string, AgentToolDescriptor>(StringComparer.Ordinal);

        foreach (var declared in response.Tools)
        {
            if (this.Offer(declared, settings) is { } descriptor)
            {
                offered.Add(descriptor);
                index.TryAdd(descriptor.Name, descriptor);
            }
        }

        var listing = new Listing(offered, index, DateTimeOffset.UtcNow);

        if (key is not null)
        {
            this.Remember(key, listing, settings.Lifetime);
        }

        return listing;
    }

    /// <summary>
    /// Computes the cache key for a caller.
    /// </summary>
    /// <param name="settings">This source's options.</param>
    /// <param name="context">The caller.</param>
    /// <returns>The key, or null to skip caching.</returns>
    private static string? CacheKey(GrpcAgentToolSourceOptions settings, IAgentToolSourceContext context)
    {
        if (settings.CacheKey is { } key)
        {
            return key(context);
        }

        if (context.User is not { } user)
        {
            return string.Empty;
        }

        var subject = user.FindFirst("sub")?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.Identity?.Name;

        // a caller who cannot be told apart from others is not handed their answer
        return subject is null ? null : "user:" + subject;
    }

    /// <summary>
    /// Caches a listing and evicts lapsed entries.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="listing">The listing.</param>
    /// <param name="lifetime">The cache lifetime.</param>
    private void Remember(string key, Listing listing, TimeSpan lifetime)
    {
        var now = listing.Read;

        foreach (var entry in this.listings)
        {
            if (now - entry.Value.Read > lifetime)
            {
                this.listings.TryRemove(entry);
            }
        }

        this.listings[key] = listing;
    }

    /// <summary>
    /// Converts a remote declaration to a local tool.
    /// </summary>
    /// <param name="declared">The declaration.</param>
    /// <param name="settings">This source's options.</param>
    /// <returns>The tool, or null if left out.</returns>
    /// <remarks>
    /// An unreadable schema drops the tool and is logged.
    /// </remarks>
    private AgentToolDescriptor? Offer(Contract.AgentToolDeclaration declared, GrpcAgentToolSourceOptions settings)
    {
        JsonElement? input;
        JsonElement? output;

        try
        {
            input = Schema(declared.InputSchema);
            output = Schema(declared.OutputSchema);
        }
        catch (JsonException exception)
        {
            GrpcAgentToolLog.UnreadableSchema(this.logger, exception, declared.Name);

            return null;
        }

        var declaration = new AgentToolDeclaration
        {
            Name = settings.Prefix is null ? declared.Name : settings.Prefix + declared.Name,
            Title = string.IsNullOrWhiteSpace(declared.Title) ? declared.Name : declared.Title,
            Description = declared.Description,
            IsReadOnly = declared.ReadOnly,
            IsDestructive = declared.Destructive,
            IsIdempotent = declared.Idempotent,
            IsOpenWorld = declared.OpenWorld,
            ResultKind = declared.ResultKind == Contract.AgentToolResultKind.Sequence ? AgentToolResultKind.Sequence : AgentToolResultKind.Whole,
            Labels = declared.Labels.ToDictionary(label => label.Key, object? (label) => label.Value, StringComparer.Ordinal)
        };

        var tool = new RemoteAgentTool(
            declaration,
            input ?? EmptyObjectSchema,
            output,
            this.client,
            declared.Name,
            settings.CallDeadline);

        var descriptor = new AgentToolDescriptor(tool, declaration, settings.Metadata);

        return settings.Curate is { } curate ? curate(descriptor) : descriptor;
    }

    private static readonly JsonElement EmptyObjectSchema = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();

    /// <summary>
    /// Parses a schema.
    /// </summary>
    /// <param name="json">The schema text.</param>
    /// <returns>The schema, or null if none was published.</returns>
    private static JsonElement? Schema(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        // cloned, so the element outlives the document it was read from
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// A caller's listing.
    /// </summary>
    /// <param name="Tools">The tools, in service order.</param>
    /// <param name="Index">The tools by local name.</param>
    /// <param name="Read">When the listing was fetched.</param>
    private sealed record Listing(IReadOnlyList<AgentToolDescriptor> Tools, IReadOnlyDictionary<string, AgentToolDescriptor> Index, DateTimeOffset Read);
}
