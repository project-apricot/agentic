namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A result shape.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Note">An optional note.</param>
public sealed record ProbeResult(long Id, string Name, string? Note);
