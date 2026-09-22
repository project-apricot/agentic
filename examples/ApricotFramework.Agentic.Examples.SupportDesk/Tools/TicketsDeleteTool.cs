using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Deletes a ticket outright.
/// </summary>
/// <remarks>
/// Destructive, and offered on the internal surface only - the surfaces label narrows what the
/// base class declared. Both matter and neither replaces the other: the surface decides whether a
/// third party agent is ever shown it, and the policy decides whether this caller may call it.
/// Both are filters, and both apply to the listing and the call alike.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Internal)]
public sealed class TicketsDeleteTool(SupportDeskStore store) : SupportDeskTool<TicketReference, TicketChanged>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_delete";

    /// <inheritdoc />
    public override string Title => "Delete a ticket";

    /// <inheritdoc />
    public override string Description =>
        "Permanently deletes a support ticket and every note on it. There is no undo and no record kept. " +
        "Prefer support_tickets_close, which keeps the ticket readable; delete only what should never " +
        "have been recorded.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => true;

    /// <inheritdoc />
    public override bool IsIdempotent => true;


    /// <inheritdoc />
    protected override Task<TicketChanged> ExecuteAsync(TicketReference arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (!store.Delete(arguments.Id))
        {
            throw new AgentToolNotFoundException($"No ticket {arguments.Id} exists.");
        }

        return Task.FromResult(new TicketChanged { Id = arguments.Id, Outcome = "deleted" });
    }
}
