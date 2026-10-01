using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// The authorization a registration carries.
/// </summary>
/// <param name="AuthorizeData">
/// Policy, role and authenticated-caller attributes, resolved via <see cref="IAuthorizationPolicyProvider"/>.
/// </param>
/// <param name="DeclaredRequirements">
/// Requirements stated directly, via <see cref="IAuthorizationRequirementData"/> or an <see cref="AuthorizationPolicy"/>.
/// </param>
/// <param name="AllowsAnonymous">
/// Whether <see cref="IAllowAnonymous"/> is present, waiving everything else.
/// </param>
/// <remarks>
/// Kept apart because <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
/// handles only <paramref name="AuthorizeData"/>.
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
    /// False when <see cref="AllowsAnonymous"/> is set, regardless of anything else declared.
    /// </remarks>
    public bool IsDeclared =>
        !this.AllowsAnonymous && (this.AuthorizeData.Count > 0 || this.DeclaredRequirements.Count > 0);
}
