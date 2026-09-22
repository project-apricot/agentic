using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A requirement standing in for a host's own.</summary>
/// <param name="name">The name the handler matches on.</param>
public sealed class ProbeRequirement(string name) : IAuthorizationRequirement
{
    /// <summary>Gets the name the handler matches on.</summary>
    public string Name { get; } = name;
}
