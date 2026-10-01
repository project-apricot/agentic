using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// The caller information given to a tool source.
/// </summary>
/// <remarks>
/// Narrower than <see cref="AgentToolContext"/> because listing involves no invocation, so progress
/// and per-invocation state are unavailable. Portable sources should reach host state through
/// <see cref="Services"/> rather than casting to <see cref="AgentToolContext"/>.
/// </remarks>
public interface IAgentToolSourceContext
{
    /// <summary>
    /// Gets the caller, or null where the host tracks none.
    /// </summary>
    /// <remarks>
    /// Null is not the same as anonymous; its meaning is for the host to decide.
    /// </remarks>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// Gets the call-scoped services.
    /// </summary>
    IServiceProvider Services { get; }
}
