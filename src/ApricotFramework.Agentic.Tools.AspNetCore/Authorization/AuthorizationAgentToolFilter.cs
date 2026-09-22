using System.Security.Claims;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Decides a tool from the authorization its registration carries.
/// </summary>
/// <remarks>
/// <para>
/// The reason a tool can be gated the way an endpoint is. The attribute on the tool is the same
/// attribute that would sit on a controller action, it resolves to the same requirements, and
/// those requirements reach the same handlers over the same stores - so a tool and the endpoint
/// beside it cannot drift into deciding the same caller differently.
/// </para>
/// <para>
/// A tool that declares nothing is allowed. That is what the MCP SDK does with a tool carrying no
/// authorization metadata, and it is the only answer that lets a public tool sit beside gated ones.
/// The host's <see cref="AuthorizationOptions.FallbackPolicy"/> is deliberately <em>not</em>
/// consulted: it belongs to endpoints, the transport endpoint carrying these calls has already been
/// through it, and applying it again per tool would gate a tool on a policy written for a route.
/// A host wanting every tool gated says so on the tools - or adds a filter of its own, which is
/// what filters are for.
/// </para>
/// <para>
/// <see cref="IAllowAnonymous"/> waives whatever else the registration declares, so
/// <c>[AllowAnonymous]</c> beside <c>[Authorize]</c> opens the tool. ASP.NET Core's own semantics,
/// footgun included, and the MCP SDK's too.
/// </para>
/// <para>
/// A call carrying no caller is handed an unauthenticated <see cref="ClaimsPrincipal"/> rather than
/// refused outright, so the requirements decide. Anything reached through <c>[Authorize]</c> denies
/// it - that attribute carries the deny-anonymous requirement whether it names a policy -
/// while a host's own requirement is left free to permit a call made by no one, which is how a
/// worker or a scheduler gets to run a tool.
/// </para>
/// <para>
/// The tool is passed as the authorization resource, so a handler can narrow on it - refusing
/// anything destructive to a given caller, say - without this class knowing that is happening.
/// </para>
/// </remarks>
public sealed class AuthorizationAgentToolFilter : IAgentToolFilter
{
    /// <summary>
    /// The decision point.
    /// </summary>
    private readonly IAuthorizationService authorizationService;

    /// <summary>
    /// What the registration carries.
    /// </summary>
    private readonly AgentToolAuthorizationPolicy policy;

    /// <summary>
    /// Creates a new instance of the filter.
    /// </summary>
    /// <param name="authorizationService">The decision point.</param>
    /// <param name="policy">What the registration carries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public AuthorizationAgentToolFilter(IAuthorizationService authorizationService, AgentToolAuthorizationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(policy);

        this.authorizationService = authorizationService;
        this.policy = policy;
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(context);

        // empty where the tool declares nothing, and where it declares [AllowAnonymous] over
        // whatever else it declares
        var requirements = await this.policy.ResolveAsync(tool).ConfigureAwait(false);

        if (requirements.Count == 0)
        {
            return AgentToolFilterDecision.Allow();
        }

        var user = context.User ?? new ClaimsPrincipal(new ClaimsIdentity());

        var result = await this.authorizationService.AuthorizeAsync(user, tool.Tool, requirements).ConfigureAwait(false);

        return result.Succeeded
            ? AgentToolFilterDecision.Allow()
            : AgentToolFilterDecision.Deny($"The caller does not satisfy what the tool '{tool.Name}' requires.");
    }
}
