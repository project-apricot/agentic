using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Log messages for the MCP tool source.
/// </summary>
internal static partial class McpAgentToolLog
{
    /// <summary>
    /// Logs a foreign tool that was dropped.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The rejection.</param>
    /// <param name="tool">The tool.</param>
    /// <param name="origin">The offering server.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Left out the tool {Tool} offered by {Origin}")]
    internal static partial void Rejected(ILogger logger, Exception exception, string tool, string origin);

    /// <summary>
    /// Logs an unreachable server.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="server">The server.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Could not reach the MCP server {Server}")]
    internal static partial void Unreachable(ILogger logger, Exception exception, string server);
}
