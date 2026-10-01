using Microsoft.AspNetCore.Http;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Invocation;

/// <summary>
/// Supplies the caller of the request in progress.
/// </summary>
/// <remarks>
/// An unauthenticated identity is reported as no caller. Services come from the executor's scope,
/// not the request's, because a tool call may outlive its request; a tool needing the request reads
/// <c>IHttpContextAccessor</c>, registered here for that reason.
/// </remarks>
/// <param name="accessor">The request in progress, if any.</param>
public sealed class HttpAgentToolContextFactory(IHttpContextAccessor accessor) : IAgentToolContextFactory
{
    /// <inheritdoc />
    public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopedServices);

        var user = accessor.HttpContext?.User;

        return ValueTask.FromResult(new AgentToolContext
        {
            User = user?.Identity?.IsAuthenticated == true ? user : null,
            Services = scopedServices
        });
    }
}
