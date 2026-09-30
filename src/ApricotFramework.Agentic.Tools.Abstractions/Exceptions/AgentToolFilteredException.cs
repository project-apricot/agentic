namespace ApricotFramework.Agentic.Tools.Exceptions;

/// <summary>
/// The tool is not offered to this caller, so to them it does not exist.
/// </summary>
/// <remarks>
/// <para>
/// What a filter's refusal becomes when a caller asks for a tool anyway. A filter decides what a
/// caller can see - a surface a tool is not declared for, an allowlist, a feature that is off - and
/// a tool left out of someone's listing should look, when they name it, like a tool that is not
/// there. That is why this derives from <see cref="AgentToolNotFoundException"/>: every surface that
/// already maps not-found does the right thing, and none can fall through to a fault by forgetting
/// a case.
/// </para>
/// <para>
/// The message is deliberately the same one a missing tool gets, so the caller learns nothing about
/// what exists beyond what they were shown. Why the filter refused is kept on <see cref="Reason"/>,
/// for a log rather than for the caller. Refusal by authorization - a tool that exists and the caller
/// may not use - is <see cref="AgentToolAccessDeniedException"/> instead.
/// </para>
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
    /// Creates a new instance of the exception for a tool a filter left out.
    /// </summary>
    /// <param name="toolName">The tool the caller asked for.</param>
    /// <param name="reason">Why the filter left it out.</param>
    public AgentToolFilteredException(string toolName, string reason) : base($"No tool is offered as '{toolName}'.")
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets why the filter left the tool out, or null where no reason was given.
    /// </summary>
    /// <remarks>
    /// Not part of the message on purpose. A caller told "not offered on this surface" has just
    /// learned that the tool exists; this is for whoever reads the log.
    /// </remarks>
    public string? Reason { get; }
}
