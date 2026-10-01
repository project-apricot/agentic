using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;

namespace ApricotFramework.Agentic.Tools.Grpc.Server;

/// <summary>
/// Serves this host's tools over gRPC.
/// </summary>
/// <remarks>
/// A thin projection; identity, visibility and failure handling are decided by the executor and its filters.
/// </remarks>
/// <param name="executor">The tool executor.</param>
public sealed class AgentToolGrpcService(IAgentToolExecutor executor) : AgentTools.AgentToolsBase
{
    /// <inheritdoc />
    public override async Task<ListToolsResponse> ListTools(ListToolsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var offered = await executor.GetAvailableToolsAsync(context.CancellationToken).ConfigureAwait(false);

        var response = new ListToolsResponse();

        response.Tools.AddRange(offered.Select(Describe));

        return response;
    }

    /// <inheritdoc />
    public override async Task<InvokeToolResponse> InvokeTool(InvokeToolRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var result = await executor.InvokeCompleteAsync(request.Name, Arguments(request), context.CancellationToken).ConfigureAwait(false);

            return new InvokeToolResponse { ResultJson = result };
        }
        catch (AgentToolException exception)
        {
            throw Describe(exception);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A failure mid-stream fails the call, so a truncated sequence is not mistaken for a complete one.
    /// </remarks>
    public override async Task InvokeToolStream(InvokeToolRequest request, IServerStreamWriter<InvokeToolChunk> responseStream, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await foreach (var item in executor.InvokeAsync(request.Name, Arguments(request), context.CancellationToken).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(new InvokeToolChunk { ItemJson = item }, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (AgentToolException exception)
        {
            throw Describe(exception);
        }
    }

    /// <summary>
    /// Reads the arguments, treating an empty payload as none.
    /// </summary>
    /// <param name="request">The call.</param>
    /// <returns>The arguments, or null.</returns>
    private static string? Arguments(InvokeToolRequest request) =>
        string.IsNullOrWhiteSpace(request.ArgumentsJson) ? null : request.ArgumentsJson;

    /// <summary>
    /// Converts a tool descriptor to its contract declaration.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The declaration.</returns>
    private static Contract.AgentToolDeclaration Describe(AgentToolDescriptor tool)
    {
        var declaration = tool.Declaration;

        var described = new Contract.AgentToolDeclaration
        {
            Name = declaration.Name,
            Title = declaration.Title,
            Description = declaration.Description,
            InputSchema = tool.Tool.JsonSchema.GetRawText(),
            OutputSchema = tool.Tool.ReturnJsonSchema?.GetRawText() ?? string.Empty,
            ReadOnly = declaration.IsReadOnly,
            Destructive = declaration.IsDestructive,
            Idempotent = declaration.IsIdempotent,
            OpenWorld = declaration.IsOpenWorld,
            ResultKind = declaration.ResultKind == AgentToolResultKind.Sequence
                ? Contract.AgentToolResultKind.Sequence
                : Contract.AgentToolResultKind.Whole
        };

        foreach (var label in declaration.Labels)
        {
            described.Labels[label.Key] = label.Value?.ToString() ?? string.Empty;
        }

        return described;
    }

    /// <summary>
    /// Converts a tool exception to a status.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The status, with <see cref="AgentToolTrailers"/> telling a refusal from a tool failure.</returns>
    private static RpcException Describe(AgentToolException exception) => exception switch
    {
        AgentToolNotFoundException => Refuse(StatusCode.NotFound, AgentToolTrailers.Refusals.NotFound, exception),
        AgentToolUnauthenticatedException => Refuse(StatusCode.Unauthenticated, AgentToolTrailers.Refusals.Unauthenticated, exception),
        AgentToolAccessDeniedException => Refuse(StatusCode.PermissionDenied, AgentToolTrailers.Refusals.AccessDenied, exception),
        AgentToolArgumentException => Refuse(StatusCode.InvalidArgument, AgentToolTrailers.Refusals.InvalidArguments, exception),
        AgentToolNotInvocableException => Refuse(StatusCode.Unimplemented, AgentToolTrailers.Refusals.NotInvocable, exception),
        AgentToolFailedException failed => Fail(failed),
        _ => Fail(new AgentToolFailedException(AgentToolFailureKind.Fault, exception.Message, exception))
    };

    /// <summary>
    /// Describes a framework refusal.
    /// </summary>
    /// <param name="code">The status code.</param>
    /// <param name="reason">The refusal reason.</param>
    /// <param name="exception">The refusal.</param>
    /// <returns>The status, with its trailers.</returns>
    private static RpcException Refuse(StatusCode code, string reason, AgentToolException exception) =>
        new(new Status(code, exception.Message), new Metadata
        {
            { AgentToolTrailers.Outcome, AgentToolTrailers.Refused },
            { AgentToolTrailers.Reason, reason }
        });

    /// <summary>
    /// Describes a failure of the tool's operation.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The status, with its trailers.</returns>
    private static RpcException Fail(AgentToolFailedException exception)
    {
        var (code, reason) = exception.Kind switch
        {
            AgentToolFailureKind.NotFound => (StatusCode.NotFound, AgentToolTrailers.Failures.NotFound),
            AgentToolFailureKind.Invalid => (StatusCode.InvalidArgument, AgentToolTrailers.Failures.Invalid),
            AgentToolFailureKind.Conflict => (StatusCode.FailedPrecondition, AgentToolTrailers.Failures.Conflict),
            AgentToolFailureKind.Denied => (StatusCode.PermissionDenied, AgentToolTrailers.Failures.Denied),
            AgentToolFailureKind.Unauthenticated => (StatusCode.Unauthenticated, AgentToolTrailers.Failures.Unauthenticated),
            AgentToolFailureKind.RateLimited => (StatusCode.ResourceExhausted, AgentToolTrailers.Failures.RateLimited),
            AgentToolFailureKind.Unavailable => (StatusCode.Unavailable, AgentToolTrailers.Failures.Unavailable),
            AgentToolFailureKind.Timeout => (StatusCode.DeadlineExceeded, AgentToolTrailers.Failures.Timeout),
            _ => (StatusCode.Internal, AgentToolTrailers.Failures.Fault)
        };

        return new RpcException(new Status(code, exception.Message), new Metadata
        {
            { AgentToolTrailers.Outcome, AgentToolTrailers.Failed },
            { AgentToolTrailers.Reason, reason },
            { AgentToolTrailers.Retryable, exception.IsRetryable ? "true" : "false" }
        });
    }
}
