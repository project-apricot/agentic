namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// An MCP server this host connects to.
/// </summary>
/// <remarks>
/// Specify either a command (child process) or an HTTP endpoint, not both.
/// </remarks>
public sealed class McpAgentToolServer
{
    /// <summary>
    /// Gets or sets the server's name.
    /// </summary>
    /// <remarks>
    /// Keys the connection and namespaces the server's tools.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to run a child-process server.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Gets the command arguments.
    /// </summary>
    public IList<string> Arguments { get; } = [];

    /// <summary>
    /// Gets the command's environment variables.
    /// </summary>
    public IDictionary<string, string?> Environment { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the endpoint of an HTTP server.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets the HTTP headers.
    /// </summary>
    /// <remarks>
    /// Not for credentials, which would be in configuration; per-caller tokens need a custom <see cref="IMcpClientProvider"/>.
    /// </remarks>
    public IDictionary<string, string?> Headers { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);
}
