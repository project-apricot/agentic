using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Turns what a tool declares into the requirements a caller has to satisfy.
/// </summary>
/// <remarks>
/// <para>
/// Both mechanisms, combined the way the authorization middleware combines them for an endpoint.
/// A named policy and roles come from
/// <see cref="AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable{IAuthorizeData})"/>;
/// requirements an attribute carries itself are added beside them. So
/// <c>[Authorize(Policy = "…")]</c>, <c>[Authorize(Roles = "…")]</c>, a bare
/// <c>[Authorize]</c> and a custom requirement attribute all work, together or apart.
/// </para>
/// <para>
/// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/> on the registration waives all
/// of it, and resolving then yields nothing at all.
/// </para>
/// <para>
/// <see cref="IAuthorizeData.AuthenticationSchemes"/> is ignored. Acting on it means challenging a
/// caller to authenticate again, and a tool invocation has no request to challenge - the caller
/// arrived already authenticated or did not arrive at all.
/// </para>
/// </remarks>
public class AgentToolAuthorizationPolicy
{
    /// <summary>
    /// Where a named policy is resolved from.
    /// </summary>
    private readonly IAuthorizationPolicyProvider policyProvider;

    /// <summary>
    /// The requirements already resolved for a tool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keyed by type and name together, not by type alone: every tool adapted from a function
    /// shares one type and differs only in what it is called, so a type-keyed cache would answer
    /// for the wrong tool.
    /// </para>
    /// <para>
    /// Used only when the provider says its policies can be cached. A provider that resolves a
    /// policy differently from one call to the next says so, and caching over it would decide a
    /// later call by an earlier call's answer.
    /// </para>
    /// </remarks>
    private readonly ConcurrentDictionary<(Type Type, string Name), IReadOnlyList<IAuthorizationRequirement>> cache = new();

    /// <summary>
    /// Creates a new instance of the resolver.
    /// </summary>
    /// <param name="policyProvider">Where a named policy is resolved from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyProvider"/> is null.</exception>
    public AgentToolAuthorizationPolicy(IAuthorizationPolicyProvider policyProvider)
    {
        ArgumentNullException.ThrowIfNull(policyProvider);

        this.policyProvider = policyProvider;
    }

    /// <summary>
    /// Checks whether a tool is gated at all, by an attribute or by registration.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <returns>True where something governs who may call it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    /// <remarks>
    /// Separate from resolving, because it needs no policy provider and is therefore answerable
    /// while a registry is being composed.
    /// </remarks>
    public static bool IsDeclared(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return AgentToolAuthorizationMetadata.For(tool).IsDeclared;
    }

    /// <summary>
    /// Resolves what a caller has to satisfy to invoke a tool.
    /// </summary>
    /// <param name="tool">The tool to resolve.</param>
    /// <returns>
    /// The requirements, empty where the tool declares no authorization - or waives it with
    /// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public async ValueTask<IReadOnlyList<IAuthorizationRequirement>> ResolveAsync(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (!this.policyProvider.AllowsCachingPolicies)
        {
            return await this.BuildAsync(tool).ConfigureAwait(false);
        }

        var key = (tool.Tool.GetType(), tool.Name);

        if (this.cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resolved = await this.BuildAsync(tool).ConfigureAwait(false);

        return this.cache.GetOrAdd(key, resolved);
    }

    /// <summary>
    /// Builds the requirements for a tool.
    /// </summary>
    /// <param name="tool">The tool to build for.</param>
    /// <returns>The requirements.</returns>
    private async ValueTask<IReadOnlyList<IAuthorizationRequirement>> BuildAsync(AgentToolDescriptor tool)
    {
        var declared = AgentToolAuthorizationMetadata.For(tool);

        if (!declared.IsDeclared)
        {
            return [];
        }

        var requirements = new List<IAuthorizationRequirement>(declared.DeclaredRequirements);

        var authorizeData = declared.AuthorizeData;

        if (authorizeData.Count > 0)
        {
            var policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeData).ConfigureAwait(false);

            if (policy is not null)
            {
                requirements.AddRange(policy.Requirements);
            }
        }

        return requirements;
    }
}
