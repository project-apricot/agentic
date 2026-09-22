using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a tool reports after changing something.</summary>
public sealed record TicketChanged
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket that changed.")]
    public required long Id { get; init; }

    /// <summary>What happened.</summary>
    [Description("What was done to it.")]
    public required string Outcome { get; init; }
}
