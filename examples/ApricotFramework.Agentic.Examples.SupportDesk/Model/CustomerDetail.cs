using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>A customer, as a single read reports one.</summary>
public sealed record CustomerDetail
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the customer.")]
    public required long Id { get; init; }

    /// <summary>Their name.</summary>
    [Description("The customer's name.")]
    public required string Name { get; init; }

    /// <summary>Their address.</summary>
    [Description("The email address the customer contacts the desk from.")]
    public required string Email { get; init; }

    /// <summary>Who they work for.</summary>
    [Description("The company they belong to, where they belong to one.")]
    public string? Company { get; init; }

    /// <summary>How many tickets they have raised.</summary>
    [Description("How many tickets the customer has raised in total.")]
    public required int TicketCount { get; init; }
}
