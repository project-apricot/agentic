using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// The authorization a tool's registration carries.
/// </summary>
/// <remarks>
/// Reads the descriptor's metadata, which by then holds both the attributes on the tool's type and
/// whatever <c>RequireAuthorization</c> added. The two arrive in one collection, and that is the
/// whole reason they compose rather than one replacing the other.
/// </remarks>
public static class AgentToolAuthorizationMetadata
{
    /// <summary>
    /// Gets the authorization a tool's registration carries.
    /// </summary>
    /// <param name="tool">The tool to read.</param>
    /// <returns>What it carries, of every kind.</returns>
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
