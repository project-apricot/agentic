using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a ticket search is called with.</summary>
public sealed record TicketSearchArguments
{
    /// <summary>What to look for.</summary>
    [Description("Words to look for in the subject or body. Leave unset to match every ticket.")]
    public string? Query { get; init; }

    /// <summary>Which status to keep.</summary>
    [Description("Keep only tickets in this state. Leave unset to match every state.")]
    public TicketStatus? Status { get; init; }

    /// <summary>How many to return.</summary>
    [Description("How many tickets to return, at most 50. Defaults to 10.")]
    public int Limit { get; init; } = 10;
}
