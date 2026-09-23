using Microsoft.AspNetCore.Http;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Invocation;

/// <summary>
/// The caller is whoever made the request in progress.
/// </summary>
/// <remarks>
/// <para>
/// An unauthenticated identity is reported as no caller at all, because the two mean different
/// things to a filter: null says this host has nobody in mind, which is what an anonymous request
/// amounts to, and the requirements then decide.
/// </para>
/// <para>
/// The services come from the scope the executor opened, not from the request. That looks
/// surprising and is deliberate: a tool call may outlive the request that started it - a
/// streaming result read slowly, a loop handed a listing - and a tool reaching into a disposed
/// request scope is the failure that produces. A tool that genuinely wants the request reads
/// <c>IHttpContextAccessor</c>, which is registered here for that reason.
/// </para>
/// </remarks>
/// <param name="accessor">The request in progress, where there is one.</param>
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
