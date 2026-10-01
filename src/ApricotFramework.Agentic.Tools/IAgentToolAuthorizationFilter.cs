using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Decides whether a caller may use a tool they can see.
/// </summary>
/// <remarks>
/// Runs only after every <see cref="IAgentToolFilter"/> has allowed, so a hidden tool is never
/// reported as forbidden (which would reveal it exists). A refused tool is left out of listings and
/// its call fails with <see cref="Exceptions.AgentToolAccessDeniedException"/>.
/// </remarks>
public interface IAgentToolAuthorizationFilter
{
    /// <summary>
    /// Decides whether the caller may use the tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision.</returns>
    ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default);
}
