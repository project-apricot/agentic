using ApricotFramework.Agentic.Tools.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Mcp.Client.Extensions;

/// <summary>
/// Wiring upstream MCP servers into this host's tools.
/// </summary>
/// <remarks>
/// All configuration rather than wiring: which servers a caller reaches is one answer, and the
/// source that reads them is one source. So these are chained off the builder, where the later
/// call is plainly the one that applies.
/// </remarks>
public static class McpAgentToolBuilderExtensions
{
    /// <summary>
    /// Offers the tools whatever MCP servers a caller reaches are offering.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">How foreign tools are offered here, or null for the defaults.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Needs an <see cref="IMcpClientProvider"/> to say which servers those are - either
    /// <see cref="WithStaticMcpClients(IAgentToolsBuilder, IConfiguration)"/> for a list written
    /// down once, or <see cref="WithMcpClientProvider{TProvider}"/> for a set that differs per
    /// caller.
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
    /// Says which MCP servers a caller reaches.
    /// </summary>
    /// <typeparam name="TProvider">The provider to use.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// What a multi-tenant host writes: connections per person, with credentials obtained per
    /// person. Replaces whatever came before it - there is one answer to which servers a caller
    /// reaches.
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
    /// Connects to the MCP servers named in configuration, for every caller alike.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">Where the servers are named.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolsBuilder WithStaticMcpClients(this IAgentToolsBuilder builder, IConfiguration configuration) =>
        builder.WithStaticMcpClients(configuration, McpAgentToolClientOptions.SectionName);

    /// <summary>
    /// Connects to the MCP servers named in a section of configuration, for every caller alike.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">Where the servers are named.</param>
    /// <param name="sectionName">Which section names them.</param>
    /// <returns>The same builder, for chaining.</returns>
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
    /// Connects to the MCP servers named here, for every caller alike.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">Names the servers, or null where they are named elsewhere.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// One connection per server for the life of the process, shared by every caller. A host
    /// whose servers differ per person wants <see cref="WithMcpClientProvider{TProvider}"/>
    /// instead: this one would hand everybody the same connections, which for a credentialed
    /// server is a data leak rather than an inconvenience.
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
