using ApricotFramework.Agentic.Examples.SupportDesk;
using ApricotFramework.Agentic.Tools;

namespace ApricotFramework.Agentic.Examples.Web;

/// <summary>
/// Who is asking, for this application.
/// </summary>
/// <remarks>
/// <para>
/// The seam the library leaves for a host: the caller comes from the request, and the surface -
/// which is this application's idea and not the library's - comes from the query string, because
/// that is how this demo lets one caller pretend to arrive from two places.
/// </para>
/// <para>
/// Every endpoint used to build this for itself, and every endpoint had to remember to. Now
/// there is one of these and the endpoints call the executor.
/// </para>
/// </remarks>
/// <param name="accessor">The request in progress, where there is one.</param>
public sealed class SupportDeskAgentToolContextFactory(IHttpContextAccessor accessor) : IAgentToolContextFactory
{
    /// <inheritdoc />
    public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopedServices);

        var http = accessor.HttpContext;

        var user = http?.User;

        return ValueTask.FromResult<AgentToolContext>(new SupportDeskAgentToolContext
        {
            User = user?.Identity?.IsAuthenticated == true ? user : null,
            Services = scopedServices,
            Surface = http?.Request.Query["surface"].FirstOrDefault()
        });
    }
}
