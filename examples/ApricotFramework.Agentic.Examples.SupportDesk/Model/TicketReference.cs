using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a single ticket read is called with.</summary>
public sealed record TicketReference
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket, as reported by support_tickets_list or support_tickets_search.")]
    public required long Id { get; init; }
}
