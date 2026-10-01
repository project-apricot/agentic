namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool requires a caller and there is none, or the session has expired.
/// </summary>
/// <remarks>
/// Fixed by signing in rather than by being granted access. Derives from
/// <see cref="AgentToolAccessDeniedException"/> so existing denial handling still applies.
/// </remarks>
public class AgentToolUnauthenticatedException : AgentToolAccessDeniedException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolUnauthenticatedException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolUnauthenticatedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolUnauthenticatedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
