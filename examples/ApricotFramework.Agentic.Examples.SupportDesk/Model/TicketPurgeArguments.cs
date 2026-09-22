using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What purging closed tickets is called with.</summary>
public sealed record TicketPurgeArguments
{
    /// <summary>How old is old enough.</summary>
    [Description("Purge closed tickets older than this many days. Must be at least 30.")]
    public required int OlderThanDays { get; init; }
}
