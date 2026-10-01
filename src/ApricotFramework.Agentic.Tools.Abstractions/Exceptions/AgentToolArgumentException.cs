namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The supplied arguments could not be read.
/// </summary>
/// <remarks>
/// Reported as arguments to correct rather than a fault to retry unchanged.
/// </remarks>
public class AgentToolArgumentException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolArgumentException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolArgumentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolArgumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
