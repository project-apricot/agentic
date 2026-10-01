using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Decides whether a caller can see a tool at all.
/// </summary>
/// <remarks>
/// <para>
/// Applies to both listing and invocation. A refused tool is treated as not found
/// (<see cref="Exceptions.AgentToolFilteredException"/>; the reason is logged, not returned).
/// Registering a filter is what enables it.
/// </para>
/// <para>
/// For scope (surface, allowlist, feature switch), not permission; authorization belongs in
/// <see cref="IAgentToolAuthorizationFilter"/>, which runs afterward.
/// </para>
/// </remarks>
public interface IAgentToolFilter
{
    /// <summary>
    /// Decides whether the caller can see the tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision.</returns>
    ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default);
}
