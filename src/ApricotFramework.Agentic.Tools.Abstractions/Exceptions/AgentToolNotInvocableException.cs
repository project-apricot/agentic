namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool is described here but cannot be run here.
/// </summary>
/// <remarks>
/// A descriptor carries a declaration, which a function satisfies and a bare description does
/// not. A catalogue that lists what a fleet offers without holding transport to any of it is a
/// legitimate thing to build, so listing such a tool is allowed, and calling it is this.
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
    /// <param name="message">What went wrong.</param>
    public AgentToolNotInvocableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">What caused it.</param>
    public AgentToolNotInvocableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
