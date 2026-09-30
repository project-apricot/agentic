using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// A tool another service owns, callable from here.
/// </summary>
/// <remarks>
/// <para>
/// The declaration came off the wire and the call goes back over it. Nothing about the tool is
/// local, which is the point: local or remote is a property of the caller, not of the operation,
/// so this sits in a registry beside a tool written in this process and nothing downstream can
/// tell the difference.
/// </para>
/// <para>
/// The call is point to point. Whoever federates several services holds a client per service and
/// reaches each directly, rather than routing through something that would need credentials for
/// all of them.
/// </para>
/// </remarks>
public sealed class RemoteAgentTool : AgentTool
{
    /// <summary>
    /// What the serving host said about the tool.
    /// </summary>
    private readonly AgentToolDeclaration declaration;

    /// <summary>
    /// Where the call goes.
    /// </summary>
    private readonly AgentTools.AgentToolsClient client;

    /// <summary>
    /// What the serving host calls the tool, which is not always what this host offers it as.
    /// </summary>
    private readonly string remoteName;

    /// <summary>
    /// Creates a new instance of the tool.
    /// </summary>
    /// <param name="declaration">What the serving host said about the tool.</param>
    /// <param name="inputSchema">The schema of the arguments.</param>
    /// <param name="outputSchema">The schema of the result, or null where none was published.</param>
    /// <param name="client">Where the call goes.</param>
    /// <param name="remoteName">What the serving host calls it.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
    public RemoteAgentTool(
        AgentToolDeclaration declaration,
        JsonElement inputSchema,
        JsonElement? outputSchema,
        AgentTools.AgentToolsClient client,
        string remoteName)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);

        this.declaration = declaration;
        this.JsonSchema = inputSchema;
        this.ReturnJsonSchema = outputSchema;
        this.client = client;
        this.remoteName = remoteName;
    }

    /// <inheritdoc />
    public override string Name => this.declaration.Name;

    /// <inheritdoc />
    public override string Title => this.declaration.Title;

    /// <inheritdoc />
    public override string Description => this.declaration.Description;

    /// <inheritdoc />
    public override bool IsReadOnly => this.declaration.IsReadOnly;

    /// <inheritdoc />
    public override bool IsDestructive => this.declaration.IsDestructive;

    /// <inheritdoc />
    public override bool IsIdempotent => this.declaration.IsIdempotent;

    /// <inheritdoc />
    /// <remarks>
    /// True whatever the serving host said. A tool reached over a wire is outside this
    /// application by construction.
    /// </remarks>
    public override bool IsOpenWorld => true;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => this.declaration.ResultKind;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> Labels => this.declaration.Labels;

    /// <inheritdoc />
    public override JsonElement JsonSchema { get; }

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema { get; }

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var request = this.Request(arguments);

        try
        {
            var response = await this.client.InvokeToolAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return Read(response.ResultJson);
        }
        catch (RpcException exception)
        {
            throw Describe(exception, this.Name);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeStreamingAsync(
        AIFunctionArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = this.Request(arguments);

        using var call = this.client.InvokeToolStream(request, cancellationToken: cancellationToken);

        while (true)
        {
            bool moved;

            try
            {
                moved = await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
            }
            catch (RpcException exception)
            {
                throw Describe(exception, this.Name);
            }

            if (!moved)
            {
                yield break;
            }

            yield return Read(call.ResponseStream.Current.ItemJson);
        }
    }

    /// <summary>
    /// Builds the call.
    /// </summary>
    /// <param name="arguments">The arguments as the caller built them.</param>
    /// <returns>The request.</returns>
    /// <remarks>
    /// A caller that already had JSON gets its own text sent, untouched, which is what keeps a
    /// large identifier intact across a hop it did not ask for.
    /// </remarks>
    private InvokeToolRequest Request(AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return new InvokeToolRequest
        {
            Name = this.remoteName,
            ArgumentsJson = arguments.GetPayload() is { } payload
                ? payload.GetRawText()
                : JsonSerializer.Serialize<IDictionary<string, object?>>(arguments, this.JsonSerializerOptions)
        };
    }

    /// <summary>
    /// Reads a result back as a value.
    /// </summary>
    /// <param name="json">The result as the serving host wrote it.</param>
    /// <returns>The result.</returns>
    private static JsonElement Read(string json)
    {
        // cloned, so the element outlives the document it was read from
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// Turns a remote failure back into one of this library's.
    /// </summary>
    /// <param name="exception">What the serving host said.</param>
    /// <param name="name">The tool this host offers.</param>
    /// <returns>The failure.</returns>
    /// <remarks>
    /// The status codes the serving side chose, read back the same way round, so a refusal
    /// stays a refusal across the hop rather than arriving as a fault a model will retry.
    /// </remarks>
    private static Exception Describe(RpcException exception, string name) => exception.StatusCode switch
    {
        StatusCode.NotFound => new AgentToolNotFoundException($"No tool is offered as '{name}'.", exception),
        StatusCode.PermissionDenied or StatusCode.Unauthenticated => new AgentToolAccessDeniedException(exception.Status.Detail, exception),
        StatusCode.InvalidArgument => new AgentToolArgumentException(exception.Status.Detail, exception),
        StatusCode.Unimplemented => new AgentToolNotInvocableException(exception.Status.Detail, exception),
        _ => exception
    };
}
