using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// The kinds of authorization a registration can carry.
/// </summary>
/// <param name="AuthorizeData">
/// Attributes naming a policy, naming roles, or requiring nothing beyond an authenticated caller.
/// Resolved against the host's <see cref="IAuthorizationPolicyProvider"/>.
/// </param>
/// <param name="DeclaredRequirements">
/// Requirements the registration states outright - through
/// <see cref="IAuthorizationRequirementData"/> on an attribute, or an
/// <see cref="AuthorizationPolicy"/> sitting in the metadata.
/// </param>
/// <param name="AllowsAnonymous">
/// Whether <see cref="IAllowAnonymous"/> is present, which waives everything above it.
/// </param>
/// <remarks>
/// The first two are kept apart because they are resolved differently, and because
/// <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
/// handles only the first kind - it folds in a named policy, roles, and the deny-anonymous
/// requirement, and passes over requirements an attribute carries itself. Combining the two is
/// what makes a tool decide the way an endpoint does.
/// </remarks>
public sealed record AgentToolAuthorizationAttributes(
    IReadOnlyList<IAuthorizeData> AuthorizeData,
    IReadOnlyList<IAuthorizationRequirement> DeclaredRequirements,
    bool AllowsAnonymous)
{
    /// <summary>
    /// Gets a value indicating whether anything governs who may call the tool.
    /// </summary>
    /// <remarks>
    /// False where <see cref="AllowsAnonymous"/> is set, however, much else is declared. Reading it
    /// that way is what makes <c>[AllowAnonymous]</c> both open the tool and keep the tripwire
    /// quiet about a gate the host deliberately waived. The MCP SDK folds the two together in the
    /// same place, for the same reason.
    /// </remarks>
    public bool IsDeclared =>
        !this.AllowsAnonymous && (this.AuthorizeData.Count > 0 || this.DeclaredRequirements.Count > 0);
}
