namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool ran and the operation behind it failed.
/// </summary>
/// <remarks>
/// A failure of the operation, not a missing tool (<see cref="AgentToolNotFoundException"/>). Hosts
/// map their own exceptions to it through an <c>IAgentToolExceptionTranslator</c>.
/// </remarks>
public class AgentToolFailedException : AgentToolException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolFailedException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolFailedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception for a failure of a given kind.
    /// </summary>
    /// <param name="kind">The failure kind.</param>
    /// <param name="message">The message, possibly read by a model.</param>
    /// <param name="innerException">The cause, or null.</param>
    /// <param name="retryable">Whether retrying unchanged may succeed, or null to derive from <paramref name="kind"/>.</param>
    public AgentToolFailedException(AgentToolFailureKind kind, string message, Exception? innerException = null, bool? retryable = null)
        : base(message, innerException!)
    {
        this.Kind = kind;
        this.IsRetryable = retryable ?? IsRetryableByDefault(kind);
    }

    /// <summary>
    /// Gets the failure kind.
    /// </summary>
    public AgentToolFailureKind Kind { get; }

    /// <summary>
    /// Gets whether retrying unchanged may succeed.
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Gets whether a failure kind is retryable by default.
    /// </summary>
    /// <param name="kind">The failure kind.</param>
    /// <returns>True for transient kinds.</returns>
    public static bool IsRetryableByDefault(AgentToolFailureKind kind) =>
        kind is AgentToolFailureKind.Unavailable or AgentToolFailureKind.Timeout or AgentToolFailureKind.RateLimited;
}
