using ModelContextProtocol.Client;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Supplies the MCP servers a caller reaches.
/// </summary>
/// <remarks>
/// This library owns no connections. <see cref="StaticMcpClientProvider"/> covers a fixed set;
/// multi-tenant hosts implement their own. Called on every listing, so pooling is up to the implementation.
/// </remarks>
public interface IMcpClientProvider
{
    /// <summary>
    /// Gets the servers this caller reaches.
    /// </summary>
    /// <param name="context">The caller and scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The clients; empty if none.</returns>
    ValueTask<IReadOnlyList<McpClient>> GetClientsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default);
}
