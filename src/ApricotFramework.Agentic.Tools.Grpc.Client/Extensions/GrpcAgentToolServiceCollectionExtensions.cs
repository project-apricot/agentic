using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Grpc.Client.Extensions;

/// <summary>
/// Registers another service's tools in this host.
/// </summary>
/// <remarks>
/// Call once per federated service; each gets its own source and options.
/// </remarks>
public static class GrpcAgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds a service's tools, reached through a host-defined client type.
    /// </summary>
    /// <typeparam name="TClient">A type derived from the generated client, one per service, registered with <c>AddGrpcClient&lt;TClient&gt;()</c>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the options, or null for defaults.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddGrpcAgentTools<TClient>(this IServiceCollection services, Action<GrpcAgentToolSourceOptions>? configure = null) where TClient : AgentTools.AgentToolsClient
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = Settle(configure);

        return services.AddAgentToolSource(provider => GrpcAgentToolSource.ForClient<TClient>(options, provider.GetService<ILogger<GrpcAgentToolSource>>()));
    }

    /// <summary>
    /// Adds a service's tools, reached through a named client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name registered with <c>AddGrpcClient&lt;AgentTools.AgentToolsClient&gt;(name)</c>.</param>
    /// <param name="configure">Configures the options, or null for defaults.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is blank.</exception>
    public static IServiceCollection AddGrpcAgentTools(this IServiceCollection services, string clientName, Action<GrpcAgentToolSourceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        var options = Settle(configure);

        return services.AddAgentToolSource(provider => GrpcAgentToolSource.ForClient(clientName, options, provider.GetService<ILogger<GrpcAgentToolSource>>()));
    }

    /// <summary>
    /// Builds the options for one source.
    /// </summary>
    /// <param name="configure">Configures the options, or null for defaults.</param>
    /// <returns>The options.</returns>
    private static GrpcAgentToolSourceOptions Settle(Action<GrpcAgentToolSourceOptions>? configure)
    {
        var options = new GrpcAgentToolSourceOptions();

        configure?.Invoke(options);

        return options;
    }
}
