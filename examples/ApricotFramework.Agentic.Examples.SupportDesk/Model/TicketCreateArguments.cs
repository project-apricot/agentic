using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What creating a ticket is called with.</summary>
public sealed record TicketCreateArguments
{
    /// <summary>The one line summary.</summary>
    [Description("A one line summary of what the customer needs.")]
    public required string Subject { get; init; }

    /// <summary>The detail.</summary>
    [Description("What the customer wrote, in full.")]
    public required string Body { get; init; }

    /// <summary>Who raised it.</summary>
    [Description("The identifier of the customer raising it, as reported by support_customers_list.")]
    public required long CustomerId { get; init; }
}
