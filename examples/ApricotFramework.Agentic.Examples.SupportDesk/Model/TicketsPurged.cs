using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What purging reports.</summary>
public sealed record TicketsPurged
{
    /// <summary>How many went.</summary>
    [Description("How many tickets were deleted.")]
    public required int Deleted { get; init; }
}
