namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// How tools read off upstream servers are offered here.
/// </summary>
public sealed class McpAgentToolSourceOptions
{
    /// <summary>
    /// Gets or sets how long a server's listing is held before it is asked again.
    /// </summary>
    /// <remarks>
    /// A backstop, not the mechanism. A server that adds a tool says so, and
    /// <see cref="IMcpToolInvalidation"/> is how that is heard; this is what covers a server
    /// that does not, or a notification that went missing with a dropped connection.
    /// </remarks>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets what is put in front of every foreign tool's name, or null for nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A foreign <c>search</c> will collide with, or shadow, something of yours. The registry
    /// refuses a duplicate name, which is the right failure - but a name space of your own
    /// avoids reaching it, and the server's own name is appended so two servers offering
    /// <c>search</c> do not collide with each other either.
    /// </para>
    /// <para>
    /// Null offers foreign names unchanged, which is a choice worth making deliberately.
    /// </para>
    /// </remarks>
    public string? Prefix { get; set; } = "mcp_";

    /// <summary>
    /// Gets or sets what a tool is assumed to do when the server does not say.
    /// </summary>
    /// <remarks>
    /// Defaults to destructive. The hints are optional and advisory: assuming the worst about a
    /// missing one costs a stricter gate, and assuming the best costs a deletion.
    /// </remarks>
    public bool DestructiveWhenUnstated { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to leave out a tool whose declaration this host would refuse.
    /// </summary>
    /// <remarks>
    /// On by default, and the opposite of what is right for tools you wrote. A service that will
    /// not start over a third party's malformed description is worse than one running with fewer
    /// tools - and what is dropped is logged, because a tool silently absent is the failure
    /// nobody can diagnose.
    /// </remarks>
    public bool DropRejected { get; set; } = true;

    /// <summary>
    /// Gets what is said about every tool read off an upstream server.
    /// </summary>
    /// <remarks>
    /// Where a gate goes. Nothing a foreign server hands over carries an authorization
    /// attribute, so this is the only thing standing between your callers and it - put here what
    /// <c>RequireAuthorization</c> would have added.
    /// </remarks>
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Gets or sets a last look at each tool before it is offered, or null for none.
    /// </summary>
    /// <remarks>
    /// Returning null leaves a tool out, which is how an allowlist is written: a server can add
    /// a tool after you reviewed what it offers, and naming the ones you reviewed is the only
    /// way that stays true. Returning a different descriptor is how a description is pinned -
    /// foreign prose goes straight into your model's prompt, and the server can change it after
    /// you vetted it.
    /// </remarks>
    public Func<AgentToolDescriptor, AgentToolDescriptor?>? Curate { get; set; }
}
