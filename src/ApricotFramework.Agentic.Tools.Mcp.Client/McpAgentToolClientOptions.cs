namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// The MCP servers this host connects to, for every caller alike.
/// </summary>
/// <remarks>
/// What a desktop application, a console tool or a single-tenant service has: a list written
/// down once, connected once, shared by whoever asks. A host whose servers differ per person
/// writes an <see cref="IMcpClientProvider"/> instead - this one would hand everybody the same
/// connections, which for a credentialed server is a data leak rather than an inconvenience.
/// </remarks>
public sealed class McpAgentToolClientOptions
{
    /// <summary>
    /// The configuration section this is bound from unless a host says otherwise.
    /// </summary>
    public const string SectionName = "AgentToolMcpServers";

    /// <summary>
    /// Gets the servers.
    /// </summary>
    public IList<McpAgentToolServer> Servers { get; } = [];

    /// <summary>
    /// Gets or sets how long to wait for a server that will not answer.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether a server that cannot be reached stops the host.
    /// </summary>
    /// <remarks>
    /// Off by default, and the same call <c>DropRejected</c> makes for the same reason: a
    /// service that will not run because a third party is down is worse than one running with
    /// fewer tools. What it cost is logged.
    /// </remarks>
    public bool RequireEveryServer { get; set; }
}
