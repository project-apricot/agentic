using ModelContextProtocol.Client;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Invalidates cached tool listings when a server's tools change.
/// </summary>
/// <remarks>
/// Listings are cached per connection; call this on <c>notifications/tools/list_changed</c>.
/// </remarks>
public interface IMcpToolInvalidation
{
    /// <summary>
    /// Forgets a server's cached listing.
    /// </summary>
    /// <param name="client">The server, or null for all.</param>
    void Invalidate(McpClient? client = null);
}
