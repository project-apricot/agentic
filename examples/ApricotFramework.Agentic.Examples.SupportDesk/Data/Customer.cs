namespace ApricotFramework.Agentic.Examples.SupportDesk.Data;

/// <summary>A customer as the store holds one.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">Their name.</param>
/// <param name="Email">Their address.</param>
/// <param name="Company">Who they work for.</param>
public sealed record Customer(long Id, string Name, string Email, string? Company);
