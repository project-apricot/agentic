using System.Security.Claims;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Authorizes a tool call against the authorization its registration carries.
/// </summary>
/// <remarks>
/// <para>
/// Runs after visibility filters; a refusal is access denied, not not-found. A tool declaring
/// nothing is allowed and <see cref="AuthorizationOptions.FallbackPolicy"/> is not consulted.
/// <see cref="IAllowAnonymous"/> waives everything else declared.
/// </para>
/// <para>
/// A call with no caller is evaluated as an unauthenticated <see cref="ClaimsPrincipal"/>, so the
/// requirements decide. The tool is passed as the authorization resource. Where a tool names
/// authentication schemes and there is a request, the identity those schemes produce is evaluated;
/// nothing is challenged.
/// </para>
/// <para>
/// Scoped, because <see cref="IAuthorizationService"/> reaches scoped handlers.
/// </para>
/// </remarks>
public sealed class AuthorizationAgentToolFilter : IAgentToolAuthorizationFilter
{
    /// <summary>
    /// The authorization service.
    /// </summary>
    private readonly IAuthorizationService authorizationService;

    /// <summary>
    /// Resolves the tool's policy.
    /// </summary>
    private readonly AgentToolAuthorizationPolicy policy;

    /// <summary>
    /// The request in progress, if any.
    /// </summary>
    private readonly IHttpContextAccessor? accessor;

    /// <summary>
    /// Creates a new instance of the filter.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="policy">The policy resolver.</param>
    /// <param name="accessor">The request accessor, or null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="authorizationService"/> or <paramref name="policy"/> is null.</exception>
    public AuthorizationAgentToolFilter(IAuthorizationService authorizationService, AgentToolAuthorizationPolicy policy, IHttpContextAccessor? accessor = null)
    {
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(policy);

        this.authorizationService = authorizationService;
        this.policy = policy;
        this.accessor = accessor;
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(context);

        // none where the tool declares nothing, and where it declares [AllowAnonymous] over
        // whatever else it declares
        var resolvedPolicy = await this.policy.ResolvePolicyAsync(tool).ConfigureAwait(false);

        if (resolvedPolicy is null)
        {
            return AgentToolAuthorizationDecision.Allow();
        }

        var caller = await this.AuthenticateAsync(resolvedPolicy, context).ConfigureAwait(false);

        var user = caller ?? new ClaimsPrincipal(new ClaimsIdentity());

        // the descriptor as the resource, so a handler can narrow on what the tool declares -
        // that it is destructive, that it carries a label - and on what the host said about it
        var result = await this.authorizationService.AuthorizeAsync(user, tool, resolvedPolicy.Requirements).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return AgentToolAuthorizationDecision.Allow();
        }

        var reason = Describe(tool, result.Failure);

        return caller is null
            ? AgentToolAuthorizationDecision.Unauthenticated(reason)
            : AgentToolAuthorizationDecision.Deny(reason);
    }

    /// <summary>
    /// Determines the identity to authorize.
    /// </summary>
    /// <param name="resolvedPolicy">The tool's policy.</param>
    /// <param name="context">The call.</param>
    /// <returns>The caller, or null if nobody is authenticated.</returns>
    /// <remarks>
    /// Merges the identities of the policy's schemes when there is a request; a failing scheme
    /// contributes nothing. Otherwise, the context's caller.
    /// </remarks>
    private async ValueTask<ClaimsPrincipal?> AuthenticateAsync(AuthorizationPolicy resolvedPolicy, AgentToolContext context)
    {
        if (resolvedPolicy.AuthenticationSchemes.Count == 0 || this.accessor?.HttpContext is not { } http)
        {
            return context.User;
        }

        ClaimsPrincipal? merged = null;

        foreach (var scheme in resolvedPolicy.AuthenticationSchemes)
        {
            var result = await http.AuthenticateAsync(scheme).ConfigureAwait(false);

            if (result is { Succeeded: true, Principal: { } principal })
            {
                merged ??= new ClaimsPrincipal();
                merged.AddIdentities(principal.Identities);
            }
        }

        return merged;
    }

    /// <summary>
    /// Builds the denial reason.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="failure">The authorization failure, if any.</param>
    /// <returns>The reason.</returns>
    /// <remarks>
    /// Includes handler failure reasons so the caller knows which access to ask for.
    /// </remarks>
    private static string Describe(AgentToolDescriptor tool, AuthorizationFailure? failure)
    {
        var reasons = failure?.FailureReasons
            .Select(reason => reason.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        var refusal = $"The caller does not satisfy what the tool '{tool.Name}' requires.";

        return reasons.Count == 0 ? refusal : $"{refusal} {string.Join(" ", reasons)}";
    }
}
