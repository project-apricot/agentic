using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What closing a ticket is called with.</summary>
public sealed record TicketCloseArguments
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the ticket to close.")]
    public required long Id { get; init; }

    /// <summary>How it was resolved.</summary>
    [Description("How the ticket was resolved, for whoever reads it later.")]
    public required string Resolution { get; init; }
}
