using ApricotFramework.Agentic.Examples.SupportDesk.Model;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Data;

/// <summary>A ticket as the store holds one.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Subject">The summary.</param>
/// <param name="Body">The detail.</param>
/// <param name="Status">Where it is in its life.</param>
/// <param name="Assignee">Who owns it.</param>
/// <param name="CustomerId">Who raised it.</param>
/// <param name="Resolution">How it was resolved.</param>
/// <param name="Notes">The notes on it.</param>
/// <param name="Raised">When it was raised.</param>
public sealed record Ticket(
    long Id,
    string Subject,
    string Body,
    TicketStatus Status,
    string? Assignee,
    long CustomerId,
    string? Resolution,
    IReadOnlyList<string> Notes,
    DateTime Raised);
