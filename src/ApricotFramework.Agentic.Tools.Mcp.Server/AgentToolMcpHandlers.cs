using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ApricotFramework.Agentic.Tools.Mcp.Server;

/// <summary>
/// What answers <c>tools/list</c> and <c>tools/call</c>.
/// </summary>
/// <remarks>
/// <para>
/// Handlers rather than a registered tool collection, and that is the point. A collection is
/// fixed when the server is built; a listing has to be able to differ per caller and to change
/// while the process runs, which is what a registry over upstream servers is for.
/// </para>
/// <para>
/// Each call goes through <see cref="IAgentToolExecutor"/>, so it opens its own scope, has its
/// own invocation context, and passes the same filters the listing passed.
/// </para>
/// </remarks>
/// <param name="executor">Where a listing and a call go.</param>
public sealed class AgentToolMcpHandlers(IAgentToolExecutor executor)
{
    /// <summary>
    /// Answers <c>tools/list</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing what this caller is offered.</returns>
    public async ValueTask<ListToolsResult> ListAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        var offered = await this.OfferedAsync(cancellationToken).ConfigureAwait(false);

        return new ListToolsResult { Tools = [.. offered.Select(tool => tool.ProtocolTool)] };
    }

    /// <summary>
    /// Answers <c>tools/call</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result.</returns>
    /// <exception cref="McpException">Thrown when the tool is unknown, refused, or called wrongly.</exception>
    /// <remarks>
    /// The listing is composed again rather than remembered, because a caller who was offered a
    /// tool an hour ago should be refused now if they would be refused now.
    /// </remarks>
    public async ValueTask<CallToolResult> CallAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Params.Name;

        var offered = await this.OfferedAsync(cancellationToken).ConfigureAwait(false);

        var tool = offered.FirstOrDefault(candidate => string.Equals(candidate.ProtocolTool.Name, name, StringComparison.Ordinal))
                   ?? throw new McpException($"No tool is offered as '{name}'.");

        try
        {
            return await tool.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AgentToolException exception)
        {
            throw Describe(exception);
        }
    }

    /// <summary>
    /// Builds the tools this caller is offered, as the protocol sees them.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    private async ValueTask<IReadOnlyList<McpServerTool>> OfferedAsync(CancellationToken cancellationToken)
    {
        var functions = await executor.GetAvailableFunctionsAsync(cancellationToken).ConfigureAwait(false);

        return [.. functions.Select(Describe)];
    }

    /// <summary>
    /// Describes one tool the way the protocol does.
    /// </summary>
    /// <param name="function">The tool.</param>
    /// <returns>The protocol's tool.</returns>
    /// <remarks>
    /// The behavior is stated rather than left to be inferred: a hint the protocol treats as
    /// advisory is still what a client decides whether to confirm on, and an unstated one reads
    /// as a tool that never said.
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
    /// Turns a failure into something a model can act on, or not retry.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The protocol's failure.</returns>
    /// <remarks>
    /// The distinction worth preserving is refusal against fault. A refusal that reads as a
    /// fault invites a model to retry it, and an agent will accept the invitation.
    /// </remarks>
    private static McpException Describe(AgentToolException exception) => exception switch
    {
        AgentToolNotFoundException => new McpException($"{exception.Message} Do not retry; list the tools again.", exception),
        AgentToolAccessDeniedException => new McpException($"{exception.Message} Do not retry.", exception),
        AgentToolArgumentException => new McpException($"{exception.Message} Correct the arguments and try again.", exception),
        _ => new McpException(exception.Message, exception)
    };
}
