namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// An authorization filter's decision for one tool and caller.
/// </summary>
/// <remarks>Distinct from <see cref="AgentToolFilterDecision"/> so the layers cannot be mixed up. <see cref="Outcome"/> distinguishes a denial from a missing caller, which are remedied differently.</remarks>
public readonly record struct AgentToolAuthorizationDecision
{
    /// <summary>
    /// Creates a decision.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="reason">The refusal reason, or null if allowed.</param>
    private AgentToolAuthorizationDecision(AgentToolAuthorizationOutcome outcome, string? reason)
    {
        this.Outcome = outcome;
        this.Reason = reason;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public AgentToolAuthorizationOutcome Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether the caller may use the tool.
    /// </summary>
    public bool IsAllowed => this.Outcome == AgentToolAuthorizationOutcome.Allowed;

    /// <summary>
    /// Gets the refusal reason, or null if allowed.
    /// </summary>
    /// <remarks>Reported to the caller, unlike a filter's reason.</remarks>
    public string? Reason { get; }

    /// <summary>
    /// Allows the caller.
    /// </summary>
    /// <returns>The decision.</returns>
    public static AgentToolAuthorizationDecision Allow() => new(AgentToolAuthorizationOutcome.Allowed, null);

    /// <summary>
    /// Denies the caller.
    /// </summary>
    /// <param name="reason">Why.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is null or blank.</exception>
    public static AgentToolAuthorizationDecision Deny(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AgentToolAuthorizationDecision(AgentToolAuthorizationOutcome.Denied, reason);
    }

    /// <summary>
    /// Refuses because there is no caller.
    /// </summary>
    /// <param name="reason">Why.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is null or blank.</exception>
    public static AgentToolAuthorizationDecision Unauthenticated(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AgentToolAuthorizationDecision(AgentToolAuthorizationOutcome.Unauthenticated, reason);
    }

    /// <inheritdoc />
    public override string ToString() => this.Reason ?? "allowed";
}
