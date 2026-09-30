using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Grpc.Client.Extensions;

/// <summary>
/// Wiring another service's tools into this host's.
/// </summary>
/// <remarks>
/// <para>
/// One call per service federated. Each gets its own source and its own options, so one service
/// being down costs its tools rather than all of them, and two services can be offered under
/// different prefixes.
/// </para>
/// </remarks>
public static class GrpcAgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the tools a service is offering, reached through a client type of the host's own.
    /// </summary>
    /// <typeparam name="TClient">A type derived from the generated client, one per service, registered with <c>AddGrpcClient&lt;TClient&gt;()</c>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">How its tools are offered here, or null for the defaults.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddGrpcAgentTools<TClient>(this IServiceCollection services, Action<GrpcAgentToolSourceOptions>? configure = null) where TClient : AgentTools.AgentToolsClient
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = Settle(configure);

        return services.AddAgentToolSource(_ => GrpcAgentToolSource.ForClient<TClient>(options));
    }

    /// <summary>
    /// Adds the tools a service is offering, reached through a named client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name the client was registered under, with <c>AddGrpcClient&lt;AgentTools.AgentToolsClient&gt;(name)</c>.</param>
    /// <param name="configure">How its tools are offered here, or null for the defaults.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is blank.</exception>
    public static IServiceCollection AddGrpcAgentTools(this IServiceCollection services, string clientName, Action<GrpcAgentToolSourceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        var options = Settle(configure);

        return services.AddAgentToolSource(_ => GrpcAgentToolSource.ForClient(clientName, options));
    }

    /// <summary>
    /// Settles the options for one source.
    /// </summary>
    /// <param name="configure">How its tools are offered, or null for the defaults.</param>
    /// <returns>The options.</returns>
    /// <remarks>
    /// Settled per call and captured. One options instance shared across every federated service
    /// would give them all the last prefix configured - and the whole point of calling this twice
    /// is that the two services are offered differently.
    /// </remarks>
    private static GrpcAgentToolSourceOptions Settle(Action<GrpcAgentToolSourceOptions>? configure)
    {
        var options = new GrpcAgentToolSourceOptions();

        configure?.Invoke(options);

        return options;
    }
}
