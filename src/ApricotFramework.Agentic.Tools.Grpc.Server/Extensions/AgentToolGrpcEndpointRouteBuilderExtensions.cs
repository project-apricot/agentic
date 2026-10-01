using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ApricotFramework.Agentic.Tools.Grpc.Server.Extensions;

/// <summary>
/// Maps this host's tools onto a gRPC endpoint.
/// </summary>
/// <remarks>
/// Does not call <c>AddGrpc</c> or <c>MapGrpcReflectionService</c>; that is the host's choice.
/// </remarks>
public static class AgentToolGrpcEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the <c>apricot.agentic.v1.AgentTools</c> service.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder, for host authentication.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    public static GrpcServiceEndpointConventionBuilder MapAgentTools(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGrpcService<AgentToolGrpcService>();
    }
}
