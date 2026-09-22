using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Closes a ticket.
/// </summary>
[Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsCloseTool(SupportDeskStore store) : SupportDeskTool<TicketCloseArguments, TicketChanged>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_close";

    /// <inheritdoc />
    public override string Title => "Close a ticket";

    /// <inheritdoc />
    public override string Description =>
        "Closes a support ticket and records how it was resolved. The ticket and its notes are kept and " +
        "stay readable; this is not a deletion.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsIdempotent => true;

    /// <inheritdoc />
    protected override Task<TicketChanged> ExecuteAsync(TicketCloseArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var ticket = store.Ticket(arguments.Id)
                     ?? throw new AgentToolNotFoundException($"No ticket {arguments.Id} exists.");

        store.Replace(ticket with { Status = TicketStatus.Closed, Resolution = arguments.Resolution });

        return Task.FromResult(new TicketChanged { Id = ticket.Id, Outcome = "closed" });
    }
}
