using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What reassigning a ticket is called with.</summary>
public sealed record TicketReassignArguments
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket to reassign.")]
    public required long Id { get; init; }

    /// <summary>Who to assign it to.</summary>
    [Description("The agent to assign it to. Pass null to leave it unassigned.")]
    public string? Assignee { get; init; }
}
