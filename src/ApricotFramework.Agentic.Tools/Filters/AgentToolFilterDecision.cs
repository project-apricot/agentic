namespace ApricotFramework.Agentic.Tools.Filters;

/// <summary>
/// What a filter decided about one tool.
/// </summary>
/// <remarks>
/// <para>
/// One outcome rather than two. A denied tool is left out of a listing and refused on invocation,
/// which covers everything a filter is for - authorization, a surface a tool is not declared for,
/// an allowlist, a feature that is off.
/// </para>
/// <para>
/// What it deliberately cannot express is "listed, but not callable right now". A budget that has
/// run out, a call awaiting approval, or a rate limit are about the call rather than about what
/// exists, and belong in an <see cref="IAgentToolInvoker"/> wrapper. Reshaping a listing - capping
/// it, sorting it, hiding something deprecated but still callable - belongs there too.
/// </para>
/// </remarks>
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
    /// Carried because it is what a refusal says and what a log line records. A tool that is
    /// simply absent, with nobody able to say which filter removed it, is the failure nobody can
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
