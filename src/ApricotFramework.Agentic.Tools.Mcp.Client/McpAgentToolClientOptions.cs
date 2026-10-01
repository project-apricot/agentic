namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// The MCP servers this host connects to, shared by every caller.
/// </summary>
/// <remarks>
/// Hosts whose servers differ per caller should implement <see cref="IMcpClientProvider"/>;
/// sharing credentialed connections leaks data.
/// </remarks>
public sealed class McpAgentToolClientOptions
{
    /// <summary>
    /// The default configuration section name.
    /// </summary>
    public const string SectionName = "AgentToolMcpServers";

    /// <summary>
    /// Gets the servers.
    /// </summary>
    public IList<McpAgentToolServer> Servers { get; } = [];

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether an unreachable server stops the host.
    /// </summary>
    /// <remarks>
    /// Off by default; unreachable servers are logged.
    /// </remarks>
    public bool RequireEveryServer { get; set; }
}
