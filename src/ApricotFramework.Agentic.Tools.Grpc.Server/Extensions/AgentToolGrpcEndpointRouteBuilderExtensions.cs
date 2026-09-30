using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ApricotFramework.Agentic.Tools.Grpc.Server.Extensions;

/// <summary>
/// Putting this host's tools on a gRPC endpoint.
/// </summary>
/// <remarks>
/// Deliberately does not call <c>AddGrpc</c> or <c>MapGrpcReflectionService</c>. How this host
/// serves gRPC is its own business, and a library that decided it would be one more thing to
/// unpick.
/// </remarks>
public static class AgentToolGrpcEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the <c>apricot.agentic.v1.AgentTools</c> service.
    /// </summary>
    /// <param name="endpoints">Where to map it.</param>
    /// <returns>The endpoint, so a host can gate it the way it gates the rest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    /// <remarks>
    /// The endpoint is the right place for a host's own authentication: the tools behind it
    /// decide what a caller may reach, and this decides whether there is a caller at all.
    /// </remarks>
    public static GrpcServiceEndpointConventionBuilder MapAgentTools(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGrpcService<AgentToolGrpcService>();
    }
}
