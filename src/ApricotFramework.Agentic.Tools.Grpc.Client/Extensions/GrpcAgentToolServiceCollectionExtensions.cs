using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Grpc.Client.Extensions;

/// <summary>
/// Wiring another service's tools into this host's.
/// </summary>
public static class GrpcAgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the tools a service reached over gRPC is offering.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="client">Which client reaches it.</param>
    /// <param name="configure">How its tools are offered here, or null for the defaults.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
    /// <remarks>
    /// <para>
    /// Takes the client rather than an address, because addressing, deadlines, retries and
    /// credentials all belong on the client and none of them belong here. Register it however
    /// this fleet registers a gRPC client - by discovery, by configuration, by hand - and pass
    /// a way to resolve it.
    /// </para>
    /// <para>
    /// One call per service federated. Each gets its own source and its own options, so one
    /// service being down costs its tools rather than all of them, and two services can be
    /// offered under different prefixes.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddGrpcAgentTools(
        this IServiceCollection services,
        Func<IServiceProvider, AgentTools.AgentToolsClient> client,
        Action<GrpcAgentToolSourceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(client);

        // settled here, per call, and captured. one options instance shared across every
        // federated service would give them all the last prefix configured - and the whole point
        // of calling this twice is that the two services are offered differently
        var options = new GrpcAgentToolSourceOptions();

        configure?.Invoke(options);

        return services.AddAgentToolSource(provider => new GrpcAgentToolSource(client(provider), options));
    }
}
