using System.Runtime.CompilerServices;
using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Lists the tickets on the desk.
/// </summary>
/// <remarks>
/// A sequence tool, because a caller can read the first tickets before the last ones arrive. Note
/// that streaming at the tool boundary only helps if the source streams too - this one yields from
/// a list, so it is stream-shaped rather than streaming, which is honest about what an example can
/// demonstrate.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsListTool(SupportDeskStore store) : SupportDeskStreamTool<AgentToolNoArguments, TicketSummary>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_list";

    /// <inheritdoc />
    public override string Title => "List tickets";

    /// <inheritdoc />
    public override string Description =>
        "Lists every support ticket on the desk, newest first, with its subject, status and assignee. " +
        "Use this to see what is outstanding, or to find a ticket's identifier before reading it in full. " +
        "Use support_tickets_search instead when looking for something specific.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override async IAsyncEnumerable<TicketSummary> ExecuteAsync(
        AgentToolNoArguments arguments,
        AgentToolContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var ticket in store.Tickets())
        {
            await Task.Yield();

            yield return new TicketSummary
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Status = ticket.Status,
                Assignee = ticket.Assignee
            };
        }
    }
}
