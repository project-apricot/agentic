using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Declares what a registered tool requires of its callers.
/// </summary>
/// <remarks>
/// For tools with no class to put an attribute on. Additive: requirements here join those
/// declared on the tool's type, so both apply.
/// </remarks>
public static class AgentToolConventionBuilderExtensions
{
    /// <summary>
    /// Requires an authenticated caller.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder) =>
        builder.RequireAuthorization(new AuthorizeAttribute());

    /// <summary>
    /// Requires the named policies.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="policyNames">The policy names.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, params string[] policyNames)
    {
        ArgumentNullException.ThrowIfNull(policyNames);

        return builder.RequireAuthorization([.. policyNames.Select(name => new AuthorizeAttribute(name))]);
    }

    /// <summary>
    /// Requires what the given attributes describe.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="authorizeData">The authorization attributes.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, params IAuthorizeData[] authorizeData)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(authorizeData);

        foreach (var data in authorizeData)
        {
            builder.Metadata.Add(data);
        }

        return builder;
    }

    /// <summary>
    /// Requires the given requirements.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="requirements">The requirements.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, params IAuthorizationRequirement[] requirements)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(requirements);

        builder.Metadata.Add(new RequirementsMetadata(requirements));

        return builder;
    }

    /// <summary>
    /// Requires the given policy.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="policy">The policy.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, AuthorizationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return builder.RequireAuthorization([.. policy.Requirements]);
    }

    /// <summary>
    /// Requires a policy configured inline.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="configurePolicy">Builds the policy.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        ArgumentNullException.ThrowIfNull(configurePolicy);

        var policy = new AuthorizationPolicyBuilder();

        configurePolicy(policy);

        return builder.RequireAuthorization(policy.Build());
    }

    /// <summary>
    /// Attaches arbitrary metadata for a host's own filters to read.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="metadata">The metadata.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder WithMetadata(this IAgentToolConventionBuilder builder, params object[] metadata)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(metadata);

        foreach (var item in metadata)
        {
            builder.Metadata.Add(item);
        }

        return builder;
    }

    /// <summary>
    /// Carries requirements given directly rather than through an attribute.
    /// </summary>
    /// <param name="requirements">The requirements.</param>
    private sealed class RequirementsMetadata(IReadOnlyList<IAuthorizationRequirement> requirements) : IAuthorizationRequirementData
    {
        /// <inheritdoc />
        public IEnumerable<IAuthorizationRequirement> GetRequirements() => requirements;
    }
}
