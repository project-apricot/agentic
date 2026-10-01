namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool is filtered out for this caller, so to them, it does not exist.
/// </summary>
/// <remarks>
/// Derives from <see cref="AgentToolNotFoundException"/> and uses the same message, so the caller
/// learns nothing beyond their listing. Authorization refusals are
/// <see cref="AgentToolAccessDeniedException"/> instead.
/// </remarks>
public class AgentToolFilteredException : AgentToolNotFoundException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AgentToolFilteredException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AgentToolFilteredException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public AgentToolFilteredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception for a filtered tool.
    /// </summary>
    /// <param name="toolName">The requested tool name.</param>
    /// <param name="reason">Why the filter excluded it.</param>
    public AgentToolFilteredException(string toolName, string reason) : base($"No tool is offered as '{toolName}'.")
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets why the filter excluded the tool, or null.
    /// </summary>
    /// <remarks>
    /// For logs only; deliberately not in the message.
    /// </remarks>
    public string? Reason { get; }
}
