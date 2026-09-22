using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>A customer, as a listing reports one.</summary>
public sealed record CustomerSummary
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the customer, used to refer to them in other tools.")]
    public required long Id { get; init; }

    /// <summary>Their name.</summary>
    [Description("The customer's name.")]
    public required string Name { get; init; }

    /// <summary>Who they work for.</summary>
    [Description("The company they belong to, where they belong to one.")]
    public string? Company { get; init; }
}
