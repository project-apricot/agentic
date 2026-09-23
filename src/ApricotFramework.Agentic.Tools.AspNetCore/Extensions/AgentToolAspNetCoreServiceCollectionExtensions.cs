using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Invocation;
using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Wiring tools into an ASP.NET Core host.
/// </summary>
public static class AgentToolAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry, the invoker and the executor, with the defaults a web host wants.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder carrying the decisions where sequence is part of the meaning.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <c>AddAgentToolsCore</c> plus <see cref="WithHttpContext"/>: the caller is whoever made
    /// the request. There is no host reachable over HTTP where that is the wrong answer - a host
    /// wanting a different principal says so with <c>WithContext</c>, which replaces this
    /// because it comes later in the chain.
    /// </para>
    /// <para>
    /// Authorization is <strong>not</strong> included, and that is deliberate. It needs
    /// <c>IAuthorizationService</c>, which only exists if the host called
    /// <c>AddAuthorization</c>, and a host with genuinely open tools is a real configuration.
    /// Forgetting it is already caught: the tripwire refuses to compose a tool that declares a
    /// gate nothing enforces, which is exactly when it matters. Call
    /// <see cref="WithAuthorization"/> to add it.
    /// </para>
    /// </remarks>
    public static IAgentToolsBuilder AddAgentToolsWeb(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolsCore().WithHttpContext();
    }

    /// <summary>
    /// Takes the caller from the request in progress.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// What <see cref="AddAgentToolsWeb"/> already did. Here separately for a host that composed
    /// the core itself and wants the web answer to this one question.
    /// </remarks>
    public static IAgentToolsBuilder WithHttpContext(this IAgentToolsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHttpContextAccessor();

        return builder.WithContext<HttpAgentToolContextFactory>();
    }

    /// <summary>
    /// Gates tools on the authorization they declare, decided through <c>IAuthorizationService</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Separate from the entry point on purpose, and the same shape the MCP SDK settled on. A
    /// host with tools and no authorization is a real configuration, and one that has not called
    /// <c>AddAuthorization</c> would otherwise get an unresolvable <c>IAuthorizationService</c>
    /// instead of an answer.
    /// </para>
    /// <para>
    /// Forgetting it is caught rather than permitted: the tripwire refuses to compose a registry
    /// whose tools declare authorization nothing is enforcing.
    /// </para>
    /// </remarks>
    public static IAgentToolsBuilder WithAuthorization(this IAgentToolsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<AgentToolAuthorizationPolicy>();
        builder.Services.TryAddSingleton<AgentToolEnforcementMarker>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolFilter, AuthorizationAgentToolFilter>());

        return builder;
    }
}
