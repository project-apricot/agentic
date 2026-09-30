using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>A ticket, as a listing reports one.</summary>
public sealed record TicketSummary
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket, used to refer to it in other tools.")]
    public required long Id { get; init; }

    /// <summary>The one line summary.</summary>
    [Description("The one line summary the customer gave.")]
    public required string Subject { get; init; }

    /// <summary>Where it is in its life.</summary>
    [Description("Whether the ticket is open, pending on the customer, or closed.")]
    public required TicketStatus Status { get; init; }

    /// <summary>Who owns it.</summary>
    [Description("The agent it is assigned to, where it is assigned to anyone.")]
    public string? Assignee { get; init; }
}
