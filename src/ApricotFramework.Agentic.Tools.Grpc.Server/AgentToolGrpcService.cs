using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;

namespace ApricotFramework.Agentic.Tools.Grpc.Server;

/// <summary>
/// This host's tools, over the wire.
/// </summary>
/// <remarks>
/// A thin projection and nothing more. Everything a caller could disagree with - who they are,
/// which tools they may reach, what a failure means - is decided by the executor and its
/// filters, exactly as it is for an HTTP endpoint or an MCP session.
/// </remarks>
/// <param name="executor">Where a listing and a call go.</param>
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
    /// A failure part-way through fails the call rather than closing the stream quietly, so a
    /// caller cannot mistake a truncated sequence for a complete one.
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
    /// Reads the arguments, treating an absent payload as none.
    /// </summary>
    /// <param name="request">The call.</param>
    /// <returns>The arguments, or null where there are none.</returns>
    /// <remarks>
    /// Proto3 cannot tell an unset string from an empty one, which is exactly why the schemas
    /// here are JSON and not protobuf. For this one field the two mean the same thing.
    /// </remarks>
    private static string? Arguments(InvokeToolRequest request) =>
        string.IsNullOrWhiteSpace(request.ArgumentsJson) ? null : request.ArgumentsJson;

    /// <summary>
    /// Describes one tool the way the contract does.
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
    /// Turns a failure into a status a caller can act on, or not retry.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The status.</returns>
    /// <remarks>
    /// The distinction worth preserving is refusal against fault. A refusal that reads as a
    /// fault invites a retry, and whatever is driving the agent will accept the invitation.
    /// </remarks>
    private static RpcException Describe(AgentToolException exception) => exception switch
    {
        AgentToolNotFoundException => new RpcException(new Status(StatusCode.NotFound, exception.Message)),
        AgentToolAccessDeniedException => new RpcException(new Status(StatusCode.PermissionDenied, exception.Message)),
        AgentToolArgumentException => new RpcException(new Status(StatusCode.InvalidArgument, exception.Message)),
        AgentToolNotInvocableException => new RpcException(new Status(StatusCode.Unimplemented, exception.Message)),
        _ => new RpcException(new Status(StatusCode.Internal, exception.Message))
    };
}
