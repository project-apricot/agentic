using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Adds a note to a ticket.
/// </summary>
[Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class NotesAddTool(SupportDeskStore store) : SupportDeskTool<NoteAddArguments, TicketChanged>
{
    /// <inheritdoc />
    public override string Name => "support_notes_add";

    /// <inheritdoc />
    public override string Title => "Add a note";

    /// <inheritdoc />
    public override string Description =>
        "Adds an internal note to a support ticket, visible to the desk but not to the customer. Use this " +
        "to record what was tried or why a ticket moved. Notes cannot be edited or removed afterwards.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsIdempotent => false;

    /// <inheritdoc />
    protected override Task<TicketChanged> ExecuteAsync(NoteAddArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var ticket = store.Ticket(arguments.TicketId)
                     ?? throw new AgentToolNotFoundException($"No ticket {arguments.TicketId} exists.");

        store.Replace(ticket with { Notes = [.. ticket.Notes, arguments.Note] });

        return Task.FromResult(new TicketChanged { Id = ticket.Id, Outcome = "note added" });
    }
}
