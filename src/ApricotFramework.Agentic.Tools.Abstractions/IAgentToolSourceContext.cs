using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// What a source is told about whoever is asking.
/// </summary>
/// <remarks>
/// <para>
/// Narrower than <see cref="AgentToolContext"/> on purpose. A source answers "what tools are
/// there", which is a question about the caller and not about a call - so it is told the caller
/// and nothing else. Progress and per-invocation state exist only once something is being run,
/// and a source reading them would be reading values that a listing cannot fill in.
/// </para>
/// <para>
/// <see cref="AgentToolContext"/> implements this, so a source written by the host that genuinely
/// needs the host's own context type can cast down to it. A source meant to be portable - the
/// ones this library ships - reaches the host state through <see cref="Services"/>, which is the same
/// route a tool uses.
/// </para>
/// </remarks>
public interface IAgentToolSourceContext
{
    /// <summary>
    /// Gets the caller or null where the host tracks none.
    /// </summary>
    /// <remarks>
    /// Null is different from anonymous. A host with no notion of a caller at all - a command
    /// line tool, a scheduled job acting as itself - leaves this unset, and what that means is
    /// decided by the host rather than assumed here.
    /// </remarks>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// Gets the services available for this composition.
    /// </summary>
    /// <remarks>
    /// Scoped to the call. A source that has to authenticate, reach a connection pool, or read a
    /// clock resolves it from here.
    /// </remarks>
    IServiceProvider Services { get; }
}
