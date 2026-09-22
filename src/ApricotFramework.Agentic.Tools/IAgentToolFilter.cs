using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Decides whether a caller may reach a tool.
/// </summary>
/// <remarks>
/// <para>
/// Every filter applies to both halves: a tool it denies is left out of a listing and refused on
/// invocation. That is not a convenience but the point - a listing exists to avoid offering what
/// invocation would refuse, so a filter that applied to only one of them would break the property
/// the listing is for.
/// </para>
/// <para>
/// Authorization is one of these rather than a mechanism of its own; see
/// <c>ApricotFramework.Agentic.Tools.AspNetCore</c> for the one that reads the attributes a tool
/// declares. A surface a tool is not meant for, an allowlist over a foreign source, a feature that
/// is switched off is all the same shape.
/// </para>
/// <para>
/// Registering one is what makes it apply. There is no setting, because a setting would be a
/// second way to say the same thing.
/// </para>
/// </remarks>
public interface IAgentToolFilter
{
    /// <summary>
    /// Decides whether the caller may reach the tool.
    /// </summary>
    /// <param name="tool">The tool and whatever was said about it at registration.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default);
}
