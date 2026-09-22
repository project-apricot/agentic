using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Exports every ticket, and is deliberately not offered.
/// </summary>
/// <remarks>
/// Marked with <see cref="AgentToolIgnoreAttribute"/>, so assembly scanning passes over it. A tool
/// that is a tool in every respect except that this host should not offer it - half-written, kept
/// for a test, or belonging to a surface this deployment does not serve. Registering it by hand
/// would still work; the attribute governs discovery, not registration.
/// </remarks>
[AgentToolIgnore]
[Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsExportTool(SupportDeskStore store) : SupportDeskTool<AgentToolNoArguments, IReadOnlyList<TicketDetail>>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_export";

    /// <inheritdoc />
    public override string Title => "Export every ticket";

    /// <inheritdoc />
    public override string Description => "Exports every ticket in full. Not offered by this deployment.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<TicketDetail>> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var tickets = store.Tickets()
            .Select(ticket => new TicketDetail
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
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<TicketDetail>>(tickets);
    }
}
