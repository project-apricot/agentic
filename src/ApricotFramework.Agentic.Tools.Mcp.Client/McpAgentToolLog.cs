using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// What this source reports.
/// </summary>
internal static partial class McpAgentToolLog
{
    /// <summary>
    /// A foreign tool this host would not accept.
    /// </summary>
    /// <param name="logger">Where to report it.</param>
    /// <param name="exception">Why it was refused.</param>
    /// <param name="tool">Which tool.</param>
    /// <param name="origin">Which server offered it.</param>
    /// <remarks>
    /// A warning rather than a failure, and reported rather than silent: dropping turns a loud
    /// failure into a quiet one on purpose, and a tool nobody can find is the failure nobody can
    /// diagnose.
    /// </remarks>
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Left out the tool {Tool} offered by {Origin}")]
    internal static partial void Rejected(ILogger logger, Exception exception, string tool, string origin);

    /// <summary>
    /// A server this host could not reach.
    /// </summary>
    /// <param name="logger">Where to report it.</param>
    /// <param name="exception">Why not.</param>
    /// <param name="server">Which server.</param>
    /// <remarks>
    /// A warning by default rather than a failure. A service that will not run because a third
    /// party is down is worse than one running with fewer tools - and a host that disagrees says
    /// so with <c>RequireEveryServer</c>.
    /// </remarks>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Could not reach the MCP server {Server}")]
    internal static partial void Unreachable(ILogger logger, Exception exception, string server);
}
