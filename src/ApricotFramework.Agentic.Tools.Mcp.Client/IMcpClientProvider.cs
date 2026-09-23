using ModelContextProtocol.Client;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Which MCP servers this caller reaches.
/// </summary>
/// <remarks>
/// <para>
/// The seam, and the reason this library owns no connections. A desktop application has a fixed
/// set of servers configured once - <see cref="StaticMcpClientProvider"/> is that, and needs no
/// code. A multi-tenant service has a set per person, with credentials obtained per person, and
/// writes its own; nothing here would guess either the credentials or the lifetime correctly.
/// </para>
/// <para>
/// Asked on every listing, so returning a pooled connection rather than a fresh one is the
/// implementation's business.
/// </para>
/// </remarks>
public interface IMcpClientProvider
{
    /// <summary>
    /// Gets the servers this caller reaches.
    /// </summary>
    /// <param name="context">Who is asking, and the scope this composition runs in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the clients, empty where this caller reaches none.</returns>
    ValueTask<IReadOnlyList<McpClient>> GetClientsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default);
}
