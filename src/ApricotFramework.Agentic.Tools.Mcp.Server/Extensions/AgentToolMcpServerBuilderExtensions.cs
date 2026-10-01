using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Mcp.Server.Extensions;

/// <summary>
/// Exposes this host's tools on an MCP server.
/// </summary>
public static class AgentToolMcpServerBuilderExtensions
{
    /// <summary>
    /// Offers this host's tools to the MCP session's caller.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Uses handlers rather than <c>WithTools</c>, so listing changes appear without a restart. The
    /// caller comes from the registered <c>IAgentToolContextFactory</c>; over stdio there is none.
    /// </remarks>
    public static IMcpServerBuilder WithAgentTools(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<AgentToolMcpHandlers>();

        builder.Services.Configure<ModelContextProtocol.Server.McpServerOptions>(options =>
        {
            options.Capabilities ??= new ModelContextProtocol.Protocol.ServerCapabilities();
            options.Capabilities.Tools ??= new ModelContextProtocol.Protocol.ToolsCapability();
            options.Capabilities.Tools.ListChanged = true;
        });

        return builder
            .WithListToolsHandler((request, cancellationToken) =>
                request.Services!.GetRequiredService<AgentToolMcpHandlers>().ListAsync(request, cancellationToken))
            .WithCallToolHandler((request, cancellationToken) =>
                request.Services!.GetRequiredService<AgentToolMcpHandlers>().CallAsync(request, cancellationToken));
    }
}
