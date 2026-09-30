using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Decides whether a caller can see a tool at all.
/// </summary>
/// <remarks>
/// <para>
/// Every filter applies to both halves: a tool it denies is left out of a listing and refused on
/// invocation. That is not a convenience but the point - a listing exists to avoid offering what
/// invocation would refuse, so a filter that applied to only one of them would break the property
/// the listing is for.
/// </para>
/// <para>
/// A filter is about scope, not permission: a surface a tool is not meant for, an allowlist over a
/// foreign source, a feature that is switched off. A tool a filter refuses does not exist as far as
/// this caller is concerned, so a call naming it is refused as not-found - with
/// <see cref="Exceptions.AgentToolFilteredException"/>, whose reason goes to the log rather than the
/// caller.
/// </para>
/// <para>
/// Authorization is <strong>not</strong> one of these. It is <see cref="IAgentToolAuthorizationFilter"/>,
/// which runs after every filter has allowed, so a caller is never told a tool they cannot see is
/// forbidden.
/// </para>
/// <para>
/// Registering one is what makes it apply. There is no setting, because a setting would be a
/// second way to say the same thing.
/// </para>
/// </remarks>
public interface IAgentToolFilter
{
    /// <summary>
    /// Decides whether the caller can see the tool.
    /// </summary>
    /// <param name="tool">The tool and whatever was said about it at registration.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default);
}
