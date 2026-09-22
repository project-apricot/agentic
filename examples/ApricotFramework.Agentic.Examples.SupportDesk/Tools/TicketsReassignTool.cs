using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Moves a ticket to another agent.
/// </summary>
/// <remarks>
/// A write that <em>is</em> idempotent - assigning the same agent twice leaves the same state - so
/// it leaves <see cref="AgentTool.IsIdempotent"/> alone and states only that it is not read-only.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsReassignTool(SupportDeskStore store) : SupportDeskTool<TicketReassignArguments, TicketChanged>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_reassign";

    /// <inheritdoc />
    public override string Title => "Reassign a ticket";

    /// <inheritdoc />
    public override string Description =>
        "Assigns a support ticket to an agent, or clears the assignment when given no agent. Does not " +
        "notify anyone; use support_notes_add to leave a note explaining why it moved.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsIdempotent => true;

    /// <inheritdoc />
    protected override Task<TicketChanged> ExecuteAsync(TicketReassignArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var ticket = store.Ticket(arguments.Id)
                     ?? throw new AgentToolNotFoundException($"No ticket {arguments.Id} exists.");

        store.Replace(ticket with { Assignee = arguments.Assignee });

        return Task.FromResult(new TicketChanged
        {
            Id = ticket.Id,
            Outcome = arguments.Assignee is null ? "unassigned" : $"assigned to {arguments.Assignee}"
        });
    }
}
