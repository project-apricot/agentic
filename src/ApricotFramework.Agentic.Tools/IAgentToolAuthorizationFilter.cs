using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Decides whether a caller may use a tool they can see.
/// </summary>
/// <remarks>
/// <para>
/// The second of two layers, and always the second. <see cref="IAgentToolFilter"/> decides what a
/// caller can see; this decides what they may use. Filters run first, so a tool the caller cannot
/// see is never put to authorization - which costs no policy lookup and, more importantly, leaks
/// nothing: telling someone a tool is forbidden tells them it exists.
/// </para>
/// <para>
/// Applied in both places a filter is: a tool this refuses is left out of the listing, and refused
/// on the call with <see cref="Exceptions.AgentToolAccessDeniedException"/> carrying this reason.
/// </para>
/// <para>
/// <c>AuthorizeAsync</c> rather than a shared method name, so one class cannot satisfy both layers
/// with a single method by accident, and a stack trace says which layer refused.
/// </para>
/// </remarks>
public interface IAgentToolAuthorizationFilter
{
    /// <summary>
    /// Decides whether the caller may use the tool.
    /// </summary>
    /// <param name="tool">The tool and whatever was said about it at registration.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default);
}
