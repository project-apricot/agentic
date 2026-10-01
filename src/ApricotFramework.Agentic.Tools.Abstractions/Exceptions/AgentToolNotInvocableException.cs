namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool is listed here but cannot be run here.
/// </summary>
/// <remarks>
/// A catalogue may list tools it holds no transport for; calling one throws this.
/// </remarks>
public class AgentToolNotInvocableException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolNotInvocableException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolNotInvocableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolNotInvocableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
