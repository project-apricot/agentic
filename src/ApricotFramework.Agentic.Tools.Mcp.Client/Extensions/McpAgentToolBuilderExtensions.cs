using ApricotFramework.Agentic.Tools.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Mcp.Client.Extensions;

/// <summary>
/// Registers upstream MCP servers' tools in this host.
/// </summary>
public static class McpAgentToolBuilderExtensions
{
    /// <summary>
    /// Offers the tools of the MCP servers a caller reaches.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">Configures the source options, or null for defaults.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Requires an <see cref="IMcpClientProvider"/>, e.g. via
    /// <see cref="WithStaticMcpClients(IAgentToolsBuilder, IConfiguration)"/> or <see cref="WithMcpClientProvider{TProvider}"/>.
    /// </remarks>
    public static IAgentToolsBuilder WithMcpTools(this IAgentToolsBuilder builder, Action<McpAgentToolSourceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddSingleton<McpAgentToolSource>();

        // the same instance behind both, because the thing that caches a listing is the thing
        // that has to be told the listing changed
        builder.Services.TryAddSingleton<IMcpToolInvalidation>(provider => provider.GetRequiredService<McpAgentToolSource>());

        builder.Services.AddAgentToolSource(provider => provider.GetRequiredService<McpAgentToolSource>());

        return builder;
    }

    /// <summary>
    /// Sets the provider of the MCP servers a caller reaches.
    /// </summary>
    /// <typeparam name="TProvider">The provider type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Replaces any previous provider.
    /// </remarks>
    public static IAgentToolsBuilder WithMcpClientProvider<TProvider>(this IAgentToolsBuilder builder)
        where TProvider : class, IMcpClientProvider
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.RemoveAll<IMcpClientProvider>();
        builder.Services.AddSingleton<IMcpClientProvider, TProvider>();

        return builder;
    }

    /// <summary>
    /// Connects to the MCP servers named in configuration, shared by every caller.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolsBuilder WithStaticMcpClients(this IAgentToolsBuilder builder, IConfiguration configuration) =>
        builder.WithStaticMcpClients(configuration, McpAgentToolClientOptions.SectionName);

    /// <summary>
    /// Connects to the MCP servers named in a configuration section, shared by every caller.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section name.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolsBuilder WithStaticMcpClients(this IAgentToolsBuilder builder, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        builder.Services.Configure<McpAgentToolClientOptions>(configuration.GetSection(sectionName));

        return builder.WithStaticMcpClients();
    }

    /// <summary>
    /// Connects to the given MCP servers, shared by every caller.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">Configures the servers, or null if configured elsewhere.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// For per-caller servers use <see cref="WithMcpClientProvider{TProvider}"/>; sharing credentialed connections leaks data.
    /// </remarks>
    public static IAgentToolsBuilder WithStaticMcpClients(this IAgentToolsBuilder builder, Action<McpAgentToolClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddSingleton<StaticMcpClientProvider>();
        builder.Services.TryAddSingleton<IMcpClientProvider>(provider => provider.GetRequiredService<StaticMcpClientProvider>());

        return builder;
    }
}
