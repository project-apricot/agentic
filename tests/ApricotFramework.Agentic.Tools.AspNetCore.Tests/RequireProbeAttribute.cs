using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>An attribute declaring <see cref="ProbeRequirement"/>, as a host's would.</summary>
/// <param name="name">The name to require.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireProbeAttribute(string name) : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new ProbeRequirement(name)];
}
