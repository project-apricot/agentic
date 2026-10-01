using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Reads the authorization a tool's registration carries.
/// </summary>
/// <remarks>
/// Covers both attributes on the tool's type and <c>RequireAuthorization</c> calls, which is why they compose.
/// </remarks>
public static class AgentToolAuthorizationMetadata
{
    /// <summary>
    /// Gets the authorization a tool's registration carries.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The tool's authorization metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public static AgentToolAuthorizationAttributes For(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return new AgentToolAuthorizationAttributes(
            tool.GetMetadata<IAuthorizeData>(),
            [
                .. tool.GetMetadata<IAuthorizationRequirementData>().SelectMany(data => data.GetRequirements()),
                .. tool.GetMetadata<AuthorizationPolicy>().SelectMany(policy => policy.Requirements)
            ],
            tool.GetMetadata<IAllowAnonymous>().Count > 0);
    }
}
