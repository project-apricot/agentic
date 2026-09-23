namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The base of the failures this library reports.
/// </summary>
public abstract class AgentToolException : Exception
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    protected AgentToolException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    protected AgentToolException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    protected AgentToolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
