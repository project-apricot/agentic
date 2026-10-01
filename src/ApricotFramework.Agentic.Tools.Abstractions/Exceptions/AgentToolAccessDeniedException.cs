namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool is offered to the caller, but the caller may not use it.
/// </summary>
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
