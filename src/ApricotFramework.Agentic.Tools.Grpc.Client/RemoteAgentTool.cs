using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// A tool owned by another service, invoked over gRPC.
/// </summary>
/// <remarks>
/// Holds no client: each call gets one from the call's scope, so a cached listing does not carry
/// a stale scope to later callers.
/// </remarks>
public sealed class RemoteAgentTool : AgentTool
{
    /// <summary>
    /// The serving host's declaration.
    /// </summary>
    private readonly AgentToolDeclaration declaration;

    /// <summary>
    /// Gets a client from the call's services.
    /// </summary>
    private readonly Func<IServiceProvider, AgentTools.AgentToolsClient> client;

    /// <summary>
    /// The tool's name on the serving host.
    /// </summary>
    private readonly string remoteName;

    /// <summary>
    /// The call deadline, or null to use the client's registration.
    /// </summary>
    private readonly TimeSpan? deadline;

    /// <summary>
    /// Creates a new instance of the tool.
    /// </summary>
    /// <param name="declaration">The serving host's declaration.</param>
    /// <param name="inputSchema">The arguments' schema.</param>
    /// <param name="outputSchema">The result schema, or null.</param>
    /// <param name="client">Gets a client from the call's services.</param>
    /// <param name="remoteName">The tool's name on the serving host.</param>
    /// <param name="deadline">The call deadline, or null to use the client's registration.</param>
    internal RemoteAgentTool(AgentToolDeclaration declaration, JsonElement inputSchema, JsonElement? outputSchema, Func<IServiceProvider, AgentTools.AgentToolsClient> client, string remoteName, TimeSpan? deadline = null)
    {
        this.deadline = deadline;
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
    /// Always true, whatever the serving host declared.
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
            var response = await this.Client(arguments).InvokeToolAsync(request, deadline: this.Deadline(), cancellationToken: cancellationToken).ConfigureAwait(false);

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

        using var call = this.Client(arguments).InvokeToolStream(request, deadline: this.Deadline(), cancellationToken: cancellationToken);

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
    /// Creates a client for one call.
    /// </summary>
    /// <param name="arguments">The arguments, carrying the call's services.</param>
    /// <returns>The client.</returns>
    /// <exception cref="AgentToolNotInvocableException">Thrown when the arguments carry no services.</exception>
    private AgentTools.AgentToolsClient Client(AIFunctionArguments arguments)
    {
        var services = arguments.Services ?? throw new AgentToolNotInvocableException($"The tool '{this.Name}' is reached over gRPC and needs the services of the call to make a client. Invoke it through the executor, or set AIFunctionArguments.Services.");

        return this.client(services);
    }

    /// <summary>
    /// Builds the request.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The request.</returns>
    /// <remarks>
    /// JSON supplied by the caller is sent verbatim, keeping large numbers intact.
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
    /// Parses a result.
    /// </summary>
    /// <param name="json">The result JSON.</param>
    /// <returns>The result.</returns>
    private static JsonElement Read(string json)
    {
        // cloned, so the element outlives the document it was read from
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// Computes the call deadline.
    /// </summary>
    /// <returns>The deadline, or null to use the client's registration.</returns>
    private DateTime? Deadline() => this.deadline is { } allowed ? DateTime.UtcNow + allowed : null;

    /// <summary>
    /// Translates a failed call using the serving host's trailers.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="name">The tool's local name.</param>
    /// <returns>The translated exception, or null to pass the original through.</returns>
    /// <remarks>
    /// Read from <see cref="AgentToolTrailers"/>, not the status code: a refusal is rethrown as the
    /// refusal raised; a failure as an <see cref="AgentToolFailedException"/>. An untrailed status - an
    /// endpoint's own authentication, a proxy - is read by its code where the code alone is
    /// unambiguous; an untrailed <c>NOT_FOUND</c> (tool or record?) and anything else pass through
    /// unchanged. The status is found anywhere in the inner-exception chain.
    /// </remarks>
    private static Exception? Describe(Exception exception, string name)
    {
        if (Rpc(exception) is not { } rpc)
        {
            return null;
        }

        var detail = rpc.Status.Detail;

        return rpc.Trailers.GetValue(AgentToolTrailers.Outcome) switch
        {
            AgentToolTrailers.Refused => rpc.Trailers.GetValue(AgentToolTrailers.Reason) switch
            {
                AgentToolTrailers.Refusals.NotFound => new AgentToolNotFoundException($"No tool is offered as '{name}'.", exception),
                AgentToolTrailers.Refusals.Unauthenticated => new AgentToolUnauthenticatedException(detail, exception),
                AgentToolTrailers.Refusals.AccessDenied => new AgentToolAccessDeniedException(detail, exception),
                AgentToolTrailers.Refusals.InvalidArguments => new AgentToolArgumentException(detail, exception),
                AgentToolTrailers.Refusals.NotInvocable => new AgentToolNotInvocableException(detail, exception),
                _ => null
            },
            AgentToolTrailers.Failed => new AgentToolFailedException(
                Kind(rpc.Trailers.GetValue(AgentToolTrailers.Reason)),
                detail,
                exception,
                bool.TryParse(rpc.Trailers.GetValue(AgentToolTrailers.Retryable), out var retryable) ? retryable : null),
            _ => rpc.StatusCode switch
            {
                StatusCode.Unauthenticated => new AgentToolUnauthenticatedException(detail, exception),
                StatusCode.PermissionDenied => new AgentToolAccessDeniedException(detail, exception),
                StatusCode.InvalidArgument => new AgentToolArgumentException(detail, exception),
                StatusCode.Unimplemented => new AgentToolNotInvocableException(detail, exception),
                _ => null
            }
        };
    }

    /// <summary>
    /// Maps a failure reason to its kind.
    /// </summary>
    /// <param name="reason">The reason from the trailers.</param>
    /// <returns>The kind; a fault if unrecognized.</returns>
    private static AgentToolFailureKind Kind(string? reason) => reason switch
    {
        AgentToolTrailers.Failures.NotFound => AgentToolFailureKind.NotFound,
        AgentToolTrailers.Failures.Invalid => AgentToolFailureKind.Invalid,
        AgentToolTrailers.Failures.Conflict => AgentToolFailureKind.Conflict,
        AgentToolTrailers.Failures.Denied => AgentToolFailureKind.Denied,
        AgentToolTrailers.Failures.Unauthenticated => AgentToolFailureKind.Unauthenticated,
        AgentToolTrailers.Failures.RateLimited => AgentToolFailureKind.RateLimited,
        AgentToolTrailers.Failures.Unavailable => AgentToolFailureKind.Unavailable,
        AgentToolTrailers.Failures.Timeout => AgentToolFailureKind.Timeout,
        _ => AgentToolFailureKind.Fault
    };

    /// <summary>
    /// Finds the <see cref="RpcException"/> in an exception chain.
    /// </summary>
    /// <param name="exception">The outermost exception.</param>
    /// <returns>The transport failure, or null.</returns>
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
