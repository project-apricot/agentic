namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The caller is not permitted to invoke the tool.
/// </summary>
/// <remarks>
/// Worth reporting so it reads as a decision rather than a fault, or a model retries it. A bare failure is an invitation to try again, and an agent will accept.
/// </remarks>
public class AgentToolAccessDeniedException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolAccessDeniedException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolAccessDeniedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolAccessDeniedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
