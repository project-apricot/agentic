using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>Grants the requirements whose names it was given.</summary>
/// <param name="granted">The names to allow.</param>
public sealed class ProbeHandler(params string[] granted) : AuthorizationHandler<ProbeRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProbeRequirement requirement)
    {
        if (granted.Contains(requirement.Name, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
