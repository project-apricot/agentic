namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// What a filter decided about one tool.
/// </summary>
public readonly struct AgentToolFilterDecision : IEquatable<AgentToolFilterDecision>
{
    /// <summary>
    /// Creates a decision.
    /// </summary>
    /// <param name="reason">Why the tool was denied, or null where it was not.</param>
    private AgentToolFilterDecision(string? reason)
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the caller may reach the tool.
    /// </summary>
    public bool IsAllowed => this.Reason is null;

    /// <summary>
    /// Gets why the tool was denied, or null where it was not.
    /// </summary>
    /// <remarks>
    /// Carried for the log, not the caller. A caller refused by a filter is told the tool is not
    /// offered, because to them, it is not; this is what lets whoever reads the log say which filter
    /// removed it - a tool simply absent, with nobody able to say why, is the failure nobody can
    /// diagnose.
    /// </remarks>
    public string? Reason { get; }

    /// <summary>
    /// The caller may reach the tool.
    /// </summary>
    /// <returns>The decision.</returns>
    public static AgentToolFilterDecision Allow() => new(null);

    /// <summary>
    /// The caller may not reach the tool.
    /// </summary>
    /// <param name="reason">Why not.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or blank.</exception>
    public static AgentToolFilterDecision Deny(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AgentToolFilterDecision(reason);
    }

    /// <inheritdoc />
    public bool Equals(AgentToolFilterDecision other) => string.Equals(this.Reason, other.Reason, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AgentToolFilterDecision other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Reason?.GetHashCode(StringComparison.Ordinal) ?? 0;

    /// <inheritdoc />
    public override string ToString() => this.Reason ?? "allowed";

    /// <summary>Compares two decisions.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True where they say the same thing.</returns>
    public static bool operator ==(AgentToolFilterDecision left, AgentToolFilterDecision right) => left.Equals(right);

    /// <summary>Compares two decisions.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True where they say different things.</returns>
    public static bool operator !=(AgentToolFilterDecision left, AgentToolFilterDecision right) => !left.Equals(right);
}
