using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Resolves what a tool declares into the requirements a caller must satisfy.
/// </summary>
/// <remarks>
/// Combines policies and roles via
/// <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>
/// with requirements attributes carry themselves, as the middleware does for an endpoint.
/// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/> waives all of it.
/// </remarks>
public class AgentToolAuthorizationPolicy
{
    /// <summary>
    /// The policy provider.
    /// </summary>
    private readonly IAuthorizationPolicyProvider policyProvider;

    /// <summary>
    /// Resolved policies, per descriptor and provider type.
    /// </summary>
    /// <remarks>
    /// Static because this resolver is scoped; used only when the provider allows caching.
    /// </remarks>
    private static readonly ConditionalWeakTable<AgentToolDescriptor, ConcurrentDictionary<Type, Resolved>> Cache = [];

    /// <summary>
    /// Creates a new instance of the resolver.
    /// </summary>
    /// <param name="policyProvider">The policy provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyProvider"/> is null.</exception>
    public AgentToolAuthorizationPolicy(IAuthorizationPolicyProvider policyProvider)
    {
        ArgumentNullException.ThrowIfNull(policyProvider);

        this.policyProvider = policyProvider;
    }

    /// <summary>
    /// Checks whether a tool declares any authorization.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>True if the tool is gated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    /// <remarks>
    /// Needs no policy provider, so it can be checked while composing a registry.
    /// </remarks>
    public static bool IsDeclared(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return AgentToolAuthorizationMetadata.For(tool).IsDeclared;
    }

    /// <summary>
    /// Resolves the requirements for invoking a tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The requirements; empty if none are declared or anonymous access is allowed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public async ValueTask<IReadOnlyList<IAuthorizationRequirement>> ResolveAsync(AgentToolDescriptor tool)
    {
        var policy = await this.ResolvePolicyAsync(tool).ConfigureAwait(false);

        return policy is null ? [] : [.. policy.Requirements];
    }

    /// <summary>
    /// Resolves the policy for invoking a tool, including schemes.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The policy; null if none is declared or anonymous access is allowed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public async ValueTask<AuthorizationPolicy?> ResolvePolicyAsync(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (!this.policyProvider.AllowsCachingPolicies)
        {
            return (await this.BuildAsync(tool).ConfigureAwait(false)).Policy;
        }

        var resolved = Cache.GetValue(tool, _ => new ConcurrentDictionary<Type, Resolved>());

        var key = this.policyProvider.GetType();

        if (resolved.TryGetValue(key, out var cached))
        {
            return cached.Policy;
        }

        var built = await this.BuildAsync(tool).ConfigureAwait(false);

        return resolved.GetOrAdd(key, built).Policy;
    }

    /// <summary>
    /// Builds the policy for a tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>The wrapped policy, so a null result can be cached.</returns>
    private async ValueTask<Resolved> BuildAsync(AgentToolDescriptor tool)
    {
        var declared = AgentToolAuthorizationMetadata.For(tool);

        if (!declared.IsDeclared)
        {
            return new Resolved(null);
        }

        var builder = new AuthorizationPolicyBuilder();

        var authorizeData = declared.AuthorizeData;

        if (authorizeData.Count > 0
            && await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeData).ConfigureAwait(false) is { } combined)
        {
            builder.Combine(combined);
        }

        builder.Requirements = [.. declared.DeclaredRequirements, .. builder.Requirements];

        if (builder.Requirements.Count == 0)
        {
            builder.RequireAuthenticatedUser();
        }

        return new Resolved(builder.Build());
    }

    /// <summary>
    /// A resolved policy, or its absence.
    /// </summary>
    /// <param name="Policy">The policy, or null if not gated.</param>
    private sealed record Resolved(AuthorizationPolicy? Policy);
}
