using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a single customer read is called with.</summary>
public sealed record CustomerReference
{
    /// <summary>The identifier.</summary>
    [Description("The identifier of the customer, as reported by support_customers_list.")]
    public required long Id { get; init; }
}
