using ApricotFramework.Agentic.Tools.Grpc.Contract;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// The tools another service is offering this caller, now.
/// </summary>
/// <remarks>
/// One of these per service a host federates. Each holds its own client, so addressing,
/// deadlines, retries and credentials are wired where they belong - on the client the host
/// registered - and nothing here has an opinion about any of them.
/// </remarks>
public sealed class GrpcAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// Where the listing comes from.
    /// </summary>
    private readonly AgentTools.AgentToolsClient client;

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
    /// <param name="client">Where the listing comes from.</param>
    /// <param name="options">How this service's tools are offered here.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public GrpcAgentToolSource(AgentTools.AgentToolsClient client, GrpcAgentToolSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        this.client = client;
        this.options = options;
    }

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

        var response = await this.client.ListToolsAsync(new ListToolsRequest(), cancellationToken: cancellationToken).ConfigureAwait(false);

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
        var declaration = new Tools.AgentToolDeclaration
        {
            Name = settings.Prefix is null ? declared.Name : settings.Prefix + declared.Name,
            Title = string.IsNullOrWhiteSpace(declared.Title) ? declared.Name : declared.Title,
            Description = declared.Description,
            IsReadOnly = declared.ReadOnly,
            IsDestructive = declared.Destructive,
            IsIdempotent = declared.Idempotent,
            IsOpenWorld = declared.OpenWorld,
            ResultKind = declared.ResultKind == Contract.AgentToolResultKind.Sequence
                ? AgentToolResultKind.Sequence
                : AgentToolResultKind.Whole,
            Labels = declared.Labels.ToDictionary(label => label.Key, label => (object?)label.Value, StringComparer.Ordinal)
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
