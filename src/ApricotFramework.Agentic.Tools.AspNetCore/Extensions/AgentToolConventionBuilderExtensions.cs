using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Says what a registered tool requires of its callers.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to an authorization attribute on a tool class, for the cases where there is no
/// class to put one on - a tool adapted from a function, or from a foreign server. Shaped after
/// the minimal API extensions of the same name and implemented the same way: each appends to the
/// registration's metadata, which is the only mechanism there is.
/// </para>
/// <para>
/// Additive. What is said here joins whatever the tool's type declares rather than replacing it,
/// so a tool carrying an attribute and registered with a policy requires both.
/// </para>
/// </remarks>
public static class AgentToolConventionBuilderExtensions
{
    /// <summary>
    /// Requires an authenticated caller.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder) =>
        builder.RequireAuthorization(new AuthorizeAttribute());

    /// <summary>
    /// Requires the named policies.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="policyNames">The policies the caller has to satisfy.</param>
    /// <returns>The same builder for chaining.</returns>
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
    /// <param name="authorizeData">The attributes, of the kind that would sit on an endpoint.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, params IAuthorizeData[] authorizeData)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(authorizeData);

        // added one at a time on purpose. handing the array to a params object[] lands it as a
        // single item, which nothing looking for an IAuthorizeData would ever find - and the
        // symptom is a tool that says it carries no authorization while plainly carrying some
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
    /// <param name="requirements">The requirements the caller has to satisfy.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, params IAuthorizationRequirement[] requirements)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(requirements);

        builder.Metadata.Add(new RequirementsMetadata(requirements));

        return builder;
    }

    /// <summary>
    /// Requires a policy built elsewhere.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="policy">The policy the caller has to satisfy.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, AuthorizationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return builder.RequireAuthorization([.. policy.Requirements]);
    }

    /// <summary>
    /// Requires a policy configured here.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="configurePolicy">Builds the policy.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolConventionBuilder RequireAuthorization(this IAgentToolConventionBuilder builder, Action<AuthorizationPolicyBuilder> configurePolicy)
    {
        ArgumentNullException.ThrowIfNull(configurePolicy);

        var policy = new AuthorizationPolicyBuilder();

        configurePolicy(policy);

        return builder.RequireAuthorization(policy.Build());
    }

    /// <summary>
    /// Says something about a tool that nothing in this library interprets.
    /// </summary>
    /// <param name="builder">The tool.</param>
    /// <param name="metadata">What to say.</param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// The escape hatch. A host with a filter of its own puts what that filter reads here.
    /// </remarks>
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
    /// <remarks>
    /// <see cref="IAuthorizationRequirementData"/> is how ASP.NET Core lets an attribute carry its
    /// own requirements, and reusing it here means the reading side needs no second case.
    /// </remarks>
    private sealed class RequirementsMetadata(IReadOnlyList<IAuthorizationRequirement> requirements) : IAuthorizationRequirementData
    {
        /// <inheritdoc />
        public IEnumerable<IAuthorizationRequirement> GetRequirements() => requirements;
    }
}
