using ModelContextProtocol.Client;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Says that what a server offers has changed.
/// </summary>
/// <remarks>
/// A listing is cached per connection, because asking a server what it offers on every call is
/// a round trip per call. A server that adds or removes a tool says so with
/// <c>notifications/tools/list_changed</c>, and whoever hears it calls this - which is the
/// difference between a cache and a stale listing.
/// </remarks>
public interface IMcpToolInvalidation
{
    /// <summary>
    /// Forgets what a server said it offers.
    /// </summary>
    /// <param name="client">The server, or null for all of them.</param>
    void Invalidate(McpClient? client = null);
}
