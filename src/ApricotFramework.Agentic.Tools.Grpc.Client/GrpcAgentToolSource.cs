using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// The tools another service is offering this caller, now.
/// </summary>
/// <remarks>
/// <para>
/// One of these per service a host federates. Every such service implements the same contract,
/// so the generated client type never tells them apart: a host names each client, or derives a
/// type of its own per service, and registers it however it registers any gRPC client.
/// Addressing, deadlines, retries and credentials all live on that registration, and nothing
/// here has an opinion about any of them.
/// </para>
/// <para>
/// It holds a way to get a client rather than a client. The channel beneath is cached by the
/// factory either way, so a client is cheap to make - but it is made in a scope, and whatever the
/// host configured per client (an interceptor, call credentials) reads its services from that
/// scope. A client held by a source that lives as long as the process would carry the scope of
/// whoever happened to ask first. So a client is made for each listing, from the caller's own
/// services, and dropped when the listing is read.
/// </para>
/// </remarks>
public sealed class GrpcAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// Gets a client that reaches the service, from the services of the call.
    /// </summary>
    /// <remarks>
    /// Captures a name or a type and nothing else, so holding it for the life of the process
    /// holds no service, channel or provider.
    /// </remarks>
    private readonly Func<IServiceProvider, AgentTools.AgentToolsClient> client;

    /// <summary>
    /// How this service's tools are offered here.
    /// </summary>
    /// <remarks>
    /// Held per source rather than resolved from options, because a host federating two services
    /// gives each its own prefix and its own allowlist. A single options instance shared by every
    /// source would silently give them all the last one configured.
    /// </remarks>
    private readonly GrpcAgentToolSourceOptions options;

    /// <summary>
    /// What the service last said it offers, where it is worth remembering.
    /// </summary>
    private (IReadOnlyList<AgentToolDescriptor> Tools, DateTimeOffset Read)? listing;

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="client">Gets a client that reaches the service, from the services of the call.</param>
    /// <param name="options">How this service's tools are offered here.</param>
    /// <remarks>
    /// Private, so the only ways to say how a client is found are the two that cannot hold one.
    /// </remarks>
    private GrpcAgentToolSource(Func<IServiceProvider, AgentTools.AgentToolsClient> client, GrpcAgentToolSourceOptions options)
    {
        this.client = client;
        this.options = options;
    }

    /// <summary>
    /// Creates a source reaching the service through a client type of the host's own.
    /// </summary>
    /// <typeparam name="TClient">A type derived from the generated client, one per service, registered with <c>AddGrpcClient()</c>.</typeparam>
    /// <param name="options">How this service's tools are offered here.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <remarks>
    /// A type per service is what lets registration helpers that name a client after its type
    /// register two of them. The client factory ignores namespaces when it does that, so two
    /// such types need different names, not just different namespaces.
    /// </remarks>
    public static GrpcAgentToolSource ForClient<TClient>(GrpcAgentToolSourceOptions options) where TClient : AgentTools.AgentToolsClient
    {
        ArgumentNullException.ThrowIfNull(options);

        return new GrpcAgentToolSource(Typed<TClient>, options);
    }

    /// <summary>
    /// Creates a source reaching the service through a named client.
    /// </summary>
    /// <param name="clientName">The name the client was registered under, with <c>AddGrpcClient&lt;AgentTools.AgentToolsClient(name)</c>.</param>
    /// <param name="options">How this service's tools are offered here.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is blank.</exception>
    public static GrpcAgentToolSource ForClient(string clientName, GrpcAgentToolSourceOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentNullException.ThrowIfNull(options);

        return new GrpcAgentToolSource(services => services.GetRequiredService<GrpcClientFactory>().CreateClient<AgentTools.AgentToolsClient>(clientName), options);
    }

    /// <summary>
    /// Gets a client of the host's own type.
    /// </summary>
    /// <typeparam name="TClient">The type.</typeparam>
    /// <param name="services">The services of the call.</param>
    /// <returns>The client.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type was never registered as a client.</exception>
    /// <remarks>
    /// The named path needs no such message: the factory already names a missing client itself.
    /// </remarks>
    private static AgentTools.AgentToolsClient Typed<TClient>(IServiceProvider services) where TClient : AgentTools.AgentToolsClient =>
        services.GetService<TClient>() ?? throw new InvalidOperationException($"No gRPC client of type '{typeof(TClient).Name}' is registered, so its agent tools cannot be reached. Register it with AddGrpcClient<{typeof(TClient).Name}>().");

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = this.options;

        if (settings.Lifetime > TimeSpan.Zero
            && this.listing is { } held
            && DateTimeOffset.UtcNow - held.Read <= settings.Lifetime)
        {
            return held.Tools;
        }

        var response = await this.client(context.Services).ListToolsAsync(new ListToolsRequest(), cancellationToken: cancellationToken).ConfigureAwait(false);

        var offered = new List<AgentToolDescriptor>(response.Tools.Count);

        foreach (var declared in response.Tools)
        {
            if (this.Offer(declared, settings) is { } descriptor)
            {
                offered.Add(descriptor);
            }
        }

        if (settings.Lifetime > TimeSpan.Zero)
        {
            this.listing = (offered, DateTimeOffset.UtcNow);
        }

        return offered;
    }

    /// <summary>
    /// Decides how one remote tool is offered here, or that it is not.
    /// </summary>
    /// <param name="declared">What the service said.</param>
    /// <param name="settings">How remote tools are offered here.</param>
    /// <returns>The descriptor, or null to leave it out.</returns>
    private AgentToolDescriptor? Offer(Contract.AgentToolDeclaration declared, GrpcAgentToolSourceOptions settings)
    {
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
            Schema(declared.InputSchema) ?? EmptyObjectSchema,
            Schema(declared.OutputSchema),
            this.client,
            declared.Name);

        var descriptor = new AgentToolDescriptor(tool, declaration, settings.Metadata);

        return settings.Curate is { } curate ? curate(descriptor) : descriptor;
    }

    /// <summary>
    /// The schema for a tool that publishes none: an object with nothing in it.
    /// </summary>
    private static readonly JsonElement EmptyObjectSchema = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();

    /// <summary>
    /// Reads a schema off the wire.
    /// </summary>
    /// <param name="json">The schema as text.</param>
    /// <returns>The schema, or null where none was published.</returns>
    /// <remarks>
    /// Text, because a protobuf struct carries every number as a double - and because a schema
    /// is something to pass along rather than something to compute on.
    /// </remarks>
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
}
