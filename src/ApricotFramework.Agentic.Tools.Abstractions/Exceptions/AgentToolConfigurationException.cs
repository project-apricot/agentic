namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The host is misconfigured: something a source needs was never registered.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/>. The registry always lets it through, even
/// from an external source, rather than treating it as a source failure.
/// </remarks>
public class AgentToolConfigurationException : InvalidOperationException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolConfigurationException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
