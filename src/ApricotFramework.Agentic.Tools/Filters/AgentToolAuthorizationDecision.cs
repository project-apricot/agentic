namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// What an authorization filter decided about one tool and one caller.
/// </summary>
/// <remarks>
/// <para>
/// A type of its own rather than an <see cref="AgentToolFilterDecision"/>, so the two layers cannot
/// be mixed up: a filter decides what a caller can see, an authorization filter decides what a caller
/// may use, and a refusal from each is reported differently.
/// </para>
/// <para>
/// Only allow and deny for now. Kept separate partly so it can grow - telling a caller who is not
/// authenticated from one who is authenticated and not permitted is the obvious next distinction,
/// and it belongs here rather than in the filter's decision.
/// </para>
/// </remarks>
public readonly struct AgentToolAuthorizationDecision : IEquatable<AgentToolAuthorizationDecision>
{
    /// <summary>
    /// Creates a decision.
    /// </summary>
    /// <param name="reason">Why the caller was refused, or null where they were not.</param>
    private AgentToolAuthorizationDecision(string? reason)
    {
        this.Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the caller may use the tool.
    /// </summary>
    public bool IsAllowed => this.Reason is null;

    /// <summary>
    /// Gets why the caller was refused, or null where they were not.
    /// </summary>
    /// <remarks>
    /// Reported to the caller, unlike a filter's reason: the tool was offered to them, so saying why
    /// they may not use it discloses nothing they did not already know.
    /// </remarks>
    public string? Reason { get; }

    /// <summary>
    /// The caller may use the tool.
    /// </summary>
    /// <returns>The decision.</returns>
    public static AgentToolAuthorizationDecision Allow() => new(null);

    /// <summary>
    /// The caller may not use the tool.
    /// </summary>
    /// <param name="reason">Why not.</param>
    /// <returns>The decision.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or blank.</exception>
    public static AgentToolAuthorizationDecision Deny(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new AgentToolAuthorizationDecision(reason);
    }

    /// <inheritdoc />
    public bool Equals(AgentToolAuthorizationDecision other) => string.Equals(this.Reason, other.Reason, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AgentToolAuthorizationDecision other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.Reason?.GetHashCode(StringComparison.Ordinal) ?? 0;

    /// <inheritdoc />
    public override string ToString() => this.Reason ?? "allowed";

    /// <summary>Compares two decisions.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True where they say the same thing.</returns>
    public static bool operator ==(AgentToolAuthorizationDecision left, AgentToolAuthorizationDecision right) => left.Equals(right);

    /// <summary>Compares two decisions.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True where they say different things.</returns>
    public static bool operator !=(AgentToolAuthorizationDecision left, AgentToolAuthorizationDecision right) => !left.Equals(right);
}
