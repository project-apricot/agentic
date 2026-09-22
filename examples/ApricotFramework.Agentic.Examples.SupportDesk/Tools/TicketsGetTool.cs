using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Reads one ticket in full.
/// </summary>
[Authorize(Policy = SupportDeskPolicies.TicketsRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsGetTool(SupportDeskStore store) : SupportDeskTool<TicketReference, TicketDetail>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_get";

    /// <inheritdoc />
    public override string Title => "Get a ticket";

    /// <inheritdoc />
    public override string Description =>
        "Reads one support ticket in full, including what the customer wrote, the notes the desk added " +
        "and how it was resolved. Use this after a listing or a search has narrowed to one ticket and " +
        "the summary does not carry the field you need.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<TicketDetail> ExecuteAsync(TicketReference arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var ticket = store.Ticket(arguments.Id)
                     ?? throw new AgentToolNotFoundException($"No ticket {arguments.Id} exists.");

        return Task.FromResult(new TicketDetail
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Body = ticket.Body,
            Status = ticket.Status,
            Assignee = ticket.Assignee,
            CustomerId = ticket.CustomerId,
            Resolution = ticket.Resolution,
            Notes = ticket.Notes,
            Raised = ticket.Raised
        });
    }
}
