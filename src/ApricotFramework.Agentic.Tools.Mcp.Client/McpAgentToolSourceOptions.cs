namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Options for offering tools from upstream MCP servers.
/// </summary>
public sealed class McpAgentToolSourceOptions
{
    /// <summary>
    /// Gets or sets how long a server's listing is cached.
    /// </summary>
    /// <remarks>
    /// A backstop for servers that do not notify via <see cref="IMcpToolInvalidation"/>.
    /// </remarks>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the prefix for every foreign tool's name, or null for none.
    /// </summary>
    /// <remarks>
    /// The server's name is also appended, so two servers offering the same name do not collide.
    /// </remarks>
    public string? Prefix { get; set; } = "mcp_";

    /// <summary>
    /// Gets or sets whether a tool with no destructive hint is treated as destructive.
    /// </summary>
    /// <remarks>
    /// Defaults to true: a missing hint is read as destructive.
    /// </remarks>
    public bool DestructiveWhenUnstated { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to drop a tool whose declaration this host would refuse.
    /// </summary>
    /// <remarks>
    /// On by default; dropped tools are logged.
    /// </remarks>
    public bool DropRejected { get; set; } = true;

    /// <summary>
    /// Gets metadata attached to every tool read off an upstream server.
    /// </summary>
    /// <remarks>
    /// Foreign tools carry no authorization attributes; add gating here.
    /// </remarks>
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Gets or sets a final transform for each tool before it is offered, or null for none.
    /// </summary>
    /// <remarks>
    /// Returning null leaves a tool out (an allowlist); returning a different descriptor pins its
    /// description, since a server can change it after review.
    /// </remarks>
    public Func<AgentToolDescriptor, AgentToolDescriptor?>? Curate { get; set; }
}
