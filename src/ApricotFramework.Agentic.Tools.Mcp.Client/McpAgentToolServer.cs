namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// One MCP server this host connects to.
/// </summary>
/// <remarks>
/// Either a command to run - a server that lives as a child process, which is what a desktop
/// application mostly has - or an endpoint to reach over HTTP. Saying both is a configuration
/// error rather than a preference, and is refused.
/// </remarks>
public sealed class McpAgentToolServer
{
    /// <summary>
    /// Gets or sets what this host calls the server.
    /// </summary>
    /// <remarks>
    /// Used to key the connection and to put the server's tools in a name space of their own,
    /// so two servers offering <c>search</c> do not collide.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to run, for a server that is a child process.
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Gets the arguments to run it with.
    /// </summary>
    public IList<string> Arguments { get; } = [];

    /// <summary>
    /// Gets the environment variables to run it with.
    /// </summary>
    public IDictionary<string, string?> Environment { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets where to reach the server, for one that is reached over HTTP.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets the headers to reach it with.
    /// </summary>
    /// <remarks>
    /// Deliberately not where a credential belongs. Anything here is read from configuration and
    /// therefore written down; a token obtained per caller needs an
    /// <see cref="IMcpClientProvider"/> of the host's own.
    /// </remarks>
    public IDictionary<string, string?> Headers { get; } = new Dictionary<string, string?>(StringComparer.Ordinal);
}
