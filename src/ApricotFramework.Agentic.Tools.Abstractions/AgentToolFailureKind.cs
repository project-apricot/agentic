namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Why a tool failed, in terms a caller can act on.
/// </summary>
/// <remarks>
/// Deliberately coarse, so hosts map to it consistently.
/// </remarks>
public enum AgentToolFailureKind
{
    /// <summary>
    /// A failure the caller can do nothing about.
    /// </summary>
    Fault = 0,

    /// <summary>
    /// The subject of the request does not exist (a missing tool is
    /// <see cref="Exceptions.AgentToolNotFoundException"/>).
    /// </summary>
    NotFound,

    /// <summary>
    /// The request is invalid; correcting it may succeed.
    /// </summary>
    Invalid,

    /// <summary>
    /// The request conflicts with the current state.
    /// </summary>
    Conflict,

    /// <summary>
    /// The operation refused the caller, beyond the tool's declared gate.
    /// </summary>
    Denied,

    /// <summary>
    /// No caller, or the caller's session is no longer valid.
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// Too many requests; waiting may succeed.
    /// </summary>
    RateLimited,

    /// <summary>
    /// A dependency is unreachable; retrying may succeed.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The operation timed out; retrying may succeed.
    /// </summary>
    Timeout
}
