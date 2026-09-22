using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What adding a note is called with.</summary>
public sealed record NoteAddArguments
{
    /// <summary>The ticket.</summary>
    [Description("The identifier of the ticket to add the note to.")]
    public required long TicketId { get; init; }

    /// <summary>The note.</summary>
    [Description("The note to add. Visible to the desk, not to the customer.")]
    public required string Note { get; init; }
}
