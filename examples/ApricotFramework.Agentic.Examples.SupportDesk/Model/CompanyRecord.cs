using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a company registry lookup reports.</summary>
public sealed record CompanyRecord
{
    /// <summary>The registered name.</summary>
    [Description("The name the company is registered under.")]
    public required string Name { get; init; }

    /// <summary>The registration number.</summary>
    [Description("The registration number, where the registry has one.")]
    public string? Registration { get; init; }

    /// <summary>Where it is registered.</summary>
    [Description("The country the company is registered in.")]
    public required string Country { get; init; }
}
