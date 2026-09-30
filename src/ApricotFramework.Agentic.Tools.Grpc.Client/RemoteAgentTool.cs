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
/// The call is point to point. Whoever federates several services registers a client per service
/// and reaches each directly, rather than routing through something that would need credentials
/// for all of them.
/// </para>
/// <para>
/// It holds a way to get a client, not a client. A tool can sit in a cached listing for as long as
/// the host likes, and a client held that long would carry the scope it was made in to every
/// later caller. Each call gets its own, from the services of the call's scope.
/// </para>
/// </remarks>
public sealed class RemoteAgentTool : AgentTool
{
    /// <summary>
    /// What the serving host said about the tool.
    /// </summary>
    private readonly AgentToolDeclaration declaration;

    /// <summary>
    /// Gets a client that reaches the serving host, from the services of the call.
    /// </summary>
    private readonly Func<IServiceProvider, AgentTools.AgentToolsClient> client;

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
    /// <param name="client">Gets a client that reaches the serving host, from the services of the call.</param>
    /// <param name="remoteName">What the serving host calls it.</param>
    /// <remarks>
    /// Internal, for the same reason as the source's: a tool built by hand could be given a way
    /// to get a client that holds one.
    /// </remarks>
    internal RemoteAgentTool(AgentToolDeclaration declaration, JsonElement inputSchema, JsonElement? outputSchema, Func<IServiceProvider, AgentTools.AgentToolsClient> client, string remoteName)
    {
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
            var response = await this.Client(arguments).InvokeToolAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

            return Read(response.ResultJson);
        }
        catch (Exception exception) when (Describe(exception, this.Name) is { } refusal)
        {
            throw refusal;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeStreamingAsync(
        AIFunctionArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = this.Request(arguments);

        using var call = this.Client(arguments).InvokeToolStream(request, cancellationToken: cancellationToken);

        while (true)
        {
            bool moved;

            try
            {
                moved = await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (Describe(exception, this.Name) is { } refusal)
            {
                throw refusal;
            }

            if (!moved)
            {
                yield break;
            }

            yield return Read(call.ResponseStream.Current.ItemJson);
        }
    }

    /// <summary>
    /// Makes a client for one call.
    /// </summary>
    /// <param name="arguments">The arguments, carrying the services of the call's scope.</param>
    /// <returns>The client.</returns>
    /// <exception cref="AgentToolNotInvocableException">Thrown when the arguments carry no services.</exception>
    /// <remarks>
    /// The executor always supplies them. A caller invoking the function by hand has to as well,
    /// because the client is registered in the container and there is nowhere else to get one.
    /// </remarks>
    private AgentTools.AgentToolsClient Client(AIFunctionArguments arguments)
    {
        var services = arguments.Services ?? throw new AgentToolNotInvocableException($"The tool '{this.Name}' is reached over gRPC and needs the services of the call to make a client. Invoke it through the executor, or set AIFunctionArguments.Services.");

        return this.client(services);
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
    /// Turns a remote failure back into one of this library's, where it is one this library has.
    /// </summary>
    /// <param name="exception">What the call failed with.</param>
    /// <param name="name">The tool this host offers.</param>
    /// <returns>The failure to throw instead, or null to let the original through untouched.</returns>
    /// <remarks>
    /// <para>
    /// The status codes the serving side chose, read back the same way round, so a refusal stays a
    /// refusal across the hop rather than arriving as a fault a model will retry.
    /// </para>
    /// <para>
    /// The status is looked for down the whole chain, not only at the top. A host may translate
    /// transport failures on its clients - into its own error type, container-wide, without this
    /// client's registration ever mentioning it - and a translation that keeps the original as its
    /// inner exception, as it should, still reads the same here. The translated exception becomes
    /// the inner one, so nothing the host added is lost.
    /// </para>
    /// </remarks>
    private static Exception? Describe(Exception exception, string name)
    {
        var rpc = Rpc(exception);

        return rpc?.StatusCode switch
        {
            StatusCode.NotFound => new AgentToolNotFoundException($"No tool is offered as '{name}'.", exception),
            StatusCode.PermissionDenied or StatusCode.Unauthenticated => new AgentToolAccessDeniedException(rpc.Status.Detail, exception),
            StatusCode.InvalidArgument => new AgentToolArgumentException(rpc.Status.Detail, exception),
            StatusCode.Unimplemented => new AgentToolNotInvocableException(rpc.Status.Detail, exception),
            _ => null
        };
    }

    /// <summary>
    /// Finds the transport failure in a chain of exceptions.
    /// </summary>
    /// <param name="exception">The outermost.</param>
    /// <returns>The transport failure, or null where there is none.</returns>
    private static RpcException? Rpc(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is RpcException rpc)
            {
                return rpc;
            }
        }

        return null;
    }
}
