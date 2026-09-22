using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Reports whether the desk's dependencies are reachable.
/// </summary>
/// <remarks>
/// Read only, open world, and <em>not</em> idempotent - which looks contradictory until you read
/// what idempotent means here. Calling it twice changes nothing, but it does not give the same
/// answer twice, and a caller told it was idempotent might cache the first one.
/// </remarks>
[Authorize]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Public)]
public sealed class ServiceStatusTool : SupportDeskTool<AgentToolNoArguments, ServiceStatus>
{
    /// <inheritdoc />
    public override string Name => "support_status_check";

    /// <inheritdoc />
    public override string Title => "Check service status";

    /// <inheritdoc />
    public override string Description =>
        "Checks whether the support desk's dependencies are reachable right now. The answer is only true " +
        "as of the moment it is asked, so ask again rather than reusing an earlier answer.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsOpenWorld => true;

    /// <inheritdoc />
    public override bool IsIdempotent => false;

    /// <inheritdoc />
    protected override Task<ServiceStatus> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        // pretends to check something, and pretends it is flaky, so the example has one tool whose
        // answer moves
        var degraded = DateTime.UtcNow.Second % 10 == 0 ? new[] { "mail-relay" } : [];

        return Task.FromResult(new ServiceStatus
        {
            Healthy = degraded.Length == 0,
            Degraded = degraded,
            Checked = DateTime.UtcNow
        });
    }
}
