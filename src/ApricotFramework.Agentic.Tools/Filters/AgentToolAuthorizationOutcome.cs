namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// An authorization outcome, in terms a caller can act on.
/// </summary>
public enum AgentToolAuthorizationOutcome
{
    /// <summary>
    /// The caller may use the tool.
    /// </summary>
    Allowed = 0,

    /// <summary>
    /// The caller lacks a required grant; reported as <see cref="Exceptions.AgentToolAccessDeniedException"/>.
    /// </summary>
    Denied,

    /// <summary>
    /// The tool needs a caller and there is none; reported as <see cref="Exceptions.AgentToolUnauthenticatedException"/>.
    /// </summary>
    Unauthenticated
}
