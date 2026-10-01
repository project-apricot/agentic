namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// No tool is offered under the requested name.
/// </summary>
/// <remarks>
/// Reported to a model as a correctable argument: it can look at the listing again.
/// </remarks>
public class AgentToolNotFoundException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolNotFoundException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
