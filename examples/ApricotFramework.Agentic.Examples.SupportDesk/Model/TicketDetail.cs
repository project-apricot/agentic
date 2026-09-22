using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>A ticket, as a single read reports one.</summary>
public sealed record TicketDetail
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket.")]
    public required long Id { get; init; }

    /// <summary>The one line summary.</summary>
    [Description("The one line summary the customer gave.")]
    public required string Subject { get; init; }

    /// <summary>What the customer wrote.</summary>
    [Description("What the customer wrote when they opened the ticket.")]
    public required string Body { get; init; }

    /// <summary>Where it is in its life.</summary>
    [Description("Whether the ticket is open, pending on the customer, or closed.")]
    public required TicketStatus Status { get; init; }

    /// <summary>Who owns it.</summary>
    [Description("The agent it is assigned to, where it is assigned to anyone.")]
    public string? Assignee { get; init; }

    /// <summary>Who raised it.</summary>
    [Description("The identifier of the customer who raised it.")]
    public required long CustomerId { get; init; }

    /// <summary>How it was resolved.</summary>
    [Description("How the ticket was resolved, where it has been closed.")]
    public string? Resolution { get; init; }

    /// <summary>The notes on it.</summary>
    [Description("Notes the desk added, oldest first.")]
    public required IReadOnlyList<string> Notes { get; init; }

    /// <summary>When it was raised.</summary>
    [Description("When the ticket was raised, in UTC.")]
    public required DateTime Raised { get; init; }
}
