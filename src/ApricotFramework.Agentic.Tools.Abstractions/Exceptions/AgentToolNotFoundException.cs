namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// No tool is declared under the name a caller asked for.
/// </summary>
/// <remarks>
/// Worth reporting to a model as an argument it can correct rather than as a missing record: the name came from a listing it was given, so getting one wrong is a mistake it can fix by looking again.
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
