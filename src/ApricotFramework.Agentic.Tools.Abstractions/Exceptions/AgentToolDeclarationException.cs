namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// A tool declaration is malformed, or two tools claim the same name.
/// </summary>
/// <remarks>
/// Thrown while the registry is built, i.e. at startup.
/// </remarks>
public class AgentToolDeclarationException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolDeclarationException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolDeclarationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolDeclarationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
