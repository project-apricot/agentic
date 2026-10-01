namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// A filter's decision for one tool.
/// </summary>
public readonly record struct AgentToolFilterDecision
{
    /// <summary>
    /// Creates a decision.
    /// </summary>
    /// <param name="reason">The denial reason, or null if allowed.</param>
    private AgentToolFilterDecision(string? reason)
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the caller can see the tool.
    /// </summary>
    public bool IsAllowed => this.Reason is null;

    /// <summary>
    /// Gets the denial reason, or null if allowed.
    /// </summary>
    /// <remarks>For logs only: a filtered tool is reported to the caller as not found and never reaches authorization.</remarks>
    public string? Reason { get; }

    /// <summary>
    /// Allows the caller to see the tool.
    /// </summary>
    /// <returns>The decision.</returns>
    public static AgentToolFilterDecision Allow() => new(null);

    /// <summary>
    /// Hides the tool from the caller.
    /// </summary>
    /// <param name="reason">Why.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is null or blank.</exception>
    public static AgentToolFilterDecision Deny(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AgentToolFilterDecision(reason);
    }

    /// <inheritdoc />
    public override string ToString() => this.Reason ?? "allowed";
}
