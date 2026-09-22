using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a company registry lookup is called with.</summary>
public sealed record CompanyLookupArguments
{
    /// <summary>The name to look up.</summary>
    [Description("The company name to look up in the public registry.")]
    public required string CompanyName { get; init; }
}
