namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>Where a ticket is in its life.</summary>
public enum TicketStatus
{
    /// <summary>Waiting on the desk.</summary>
    Open = 0,

    /// <summary>Waiting on the customer.</summary>
    Pending = 1,

    /// <summary>Finished.</summary>
    Closed = 2
}
