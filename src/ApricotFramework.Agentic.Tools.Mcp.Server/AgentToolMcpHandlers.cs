using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ApricotFramework.Agentic.Tools.Mcp.Server;

/// <summary>
/// Handles MCP <c>tools/list</c> and <c>tools/call</c>.
/// </summary>
/// <remarks>
/// Handlers rather than a fixed tool collection, so a listing can differ per caller and change at
/// runtime. Each call goes through <see cref="IAgentToolExecutor"/> and its filters.
/// </remarks>
/// <param name="executor">The tool executor.</param>
public sealed class AgentToolMcpHandlers(IAgentToolExecutor executor)
{
    /// <summary>
    /// Handles <c>tools/list</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools offered to this caller.</returns>
    public async ValueTask<ListToolsResult> ListAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        var offered = await this.OfferedAsync(cancellationToken).ConfigureAwait(false);

        return new ListToolsResult { Tools = [.. offered.Select(tool => tool.ProtocolTool)] };
    }

    /// <summary>
    /// Handles <c>tools/call</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; an error result if the tool was refused or failed.</returns>
    /// <exception cref="McpException">Thrown when no tool is offered to this caller under the name.</exception>
    /// <remarks>
    /// Looks up only the called tool. A missing tool is a protocol error; every other refusal or
    /// failure is an <c>isError</c> result with guidance on whether to retry.
    /// </remarks>
    public async ValueTask<CallToolResult> CallAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Params.Name;

        var function = await executor.GetAvailableFunctionAsync(name, cancellationToken).ConfigureAwait(false)
                       ?? throw new McpException($"No tool is offered as '{name}'.");

        try
        {
            return await Describe(function).InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AgentToolNotFoundException exception)
        {
            // gone between the lookup and the call; to the caller that is a tool that is not there
            throw new McpException($"{exception.Message} Do not retry; list the tools again.", exception);
        }
        catch (AgentToolException exception)
        {
            return Error(Describe(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not McpException)
        {
            // nobody described it, so its message is not known to be fit for a model; say only that
            // it failed, as the SDK does for its own tools
            return Error($"The tool '{name}' failed. Do not retry unchanged.");
        }
    }

    /// <summary>
    /// Builds an error result.
    /// </summary>
    /// <param name="text">What the model reads.</param>
    /// <returns>The result.</returns>
    private static CallToolResult Error(string text) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = text }]
    };

    /// <summary>
    /// Builds the protocol tools offered to this caller.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    private async ValueTask<IReadOnlyList<McpServerTool>> OfferedAsync(CancellationToken cancellationToken)
    {
        var functions = await executor.GetAvailableFunctionsAsync(cancellationToken).ConfigureAwait(false);

        return [.. functions.Select(Describe)];
    }

    /// <summary>
    /// Converts a tool to its protocol form.
    /// </summary>
    /// <param name="function">The tool.</param>
    /// <returns>The protocol tool.</returns>
    /// <remarks>
    /// Behavior hints are always stated explicitly, since clients decide confirmation on them.
    /// </remarks>
    private static McpServerTool Describe(AIFunction function)
    {
        var declaration = function as IAgentToolDeclaration;

        return McpServerTool.Create(function, new McpServerToolCreateOptions
        {
            Title = declaration?.Title,
            ReadOnly = declaration?.IsReadOnly,
            Destructive = declaration?.IsDestructive,
            Idempotent = declaration?.IsIdempotent,
            OpenWorld = declaration?.IsOpenWorld,
            UseStructuredContent = function.ReturnJsonSchema is not null
        });
    }

    /// <summary>
    /// Builds the error result text, including what to do next.
    /// </summary>
    /// <param name="exception">The refusal or failure.</param>
    /// <returns>The error text.</returns>
    /// <remarks>
    /// Refusals must not read as retryable.
    /// </remarks>
    private static string Describe(AgentToolException exception) => exception switch
    {
        AgentToolUnauthenticatedException => $"{exception.Message} The user is not signed in, or their session has ended; they need to sign in again. Do not retry.",
        AgentToolAccessDeniedException => $"{exception.Message} The user does not hold the access this needs. Do not retry; ask the user to request it.",
        AgentToolArgumentException => $"{exception.Message} Correct the arguments and try again.",
        AgentToolNotInvocableException => $"{exception.Message} Do not retry.",
        AgentToolFailedException failed => $"{failed.Message} {Advice(failed)}",
        _ => $"{exception.Message} Do not retry unchanged."
    };

    /// <summary>
    /// Builds the advice for an operation failure.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns>The advice.</returns>
    private static string Advice(AgentToolFailedException failure) => failure.Kind switch
    {
        AgentToolFailureKind.NotFound => "Nothing matches what was asked for; check the identifiers rather than retrying unchanged.",
        AgentToolFailureKind.Invalid => "The request was rejected as invalid; correct it and try again.",
        AgentToolFailureKind.Conflict => "The current state does not allow this; read it again before deciding whether to retry.",
        AgentToolFailureKind.Denied => "The user is not permitted to do this. Do not retry; ask the user to request access.",
        AgentToolFailureKind.Unauthenticated => "The user needs to sign in again. Do not retry.",
        AgentToolFailureKind.RateLimited => "Too many requests; wait before trying again.",
        _ when failure.IsRetryable => "This may succeed if retried in a moment.",
        _ => "Do not retry unchanged."
    };
}
