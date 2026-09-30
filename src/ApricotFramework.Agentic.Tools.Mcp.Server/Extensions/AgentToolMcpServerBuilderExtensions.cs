using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Mcp.Server.Extensions;

/// <summary>
/// Putting this host's tools on an MCP server.
/// </summary>
public static class AgentToolMcpServerBuilderExtensions
{
    /// <summary>
    /// Offers whatever tools this host composes, to whoever the MCP session belongs to.
    /// </summary>
    /// <param name="builder">The MCP server being built.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Handlers rather than <c>WithTools</c>, because a tool collection is fixed when the server
    /// is built and a listing here is not: a tool an upstream server added, or one this caller
    /// may no longer reach, shows up on the next <c>tools/list</c> without a restart.
    /// </para>
    /// <para>
    /// Who the caller is comes from whatever <c>IAgentToolContextFactory</c> the host
    /// registered. Over HTTP that is <c>AddHttpAgentToolContext()</c> from the ASP.NET Core
    /// package, which reads the request's principal; over stdio there is no caller, and the
    /// default says so rather than inventing one.
    /// </para>
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
