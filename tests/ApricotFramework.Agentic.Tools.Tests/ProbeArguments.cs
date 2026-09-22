using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>An argument shape exercising descriptions and nullability.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Filter">An optional filter.</param>
public sealed record ProbeArguments(
    [property: Description("The identifier to read.")] long Id,
    [property: Description("An optional filter.")] string? Filter = null);
