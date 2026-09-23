namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// A tool's declaration is malformed, or two tools claim the same name.
/// </summary>
/// <remarks>
/// Thrown while the registry is being built, which is to say at startup. A tool nothing can invoke
/// correctly is worth refusing to start over, rather than advertising in a listing a consumer has
/// already cached by the time anyone notices.
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
