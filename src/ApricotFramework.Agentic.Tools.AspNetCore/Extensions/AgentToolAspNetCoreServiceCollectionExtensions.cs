using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Invocation;
using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Registers agent tools in an ASP.NET Core host.
/// </summary>
public static class AgentToolAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry, invoker and executor, taking the caller from the request.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The tools' builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Authorization is not included; call <see cref="WithAuthorization"/>. A later <c>WithContext</c>
    /// replaces <see cref="WithHttpContext"/>.
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
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Already applied by <see cref="AddAgentToolsWeb"/>.
    /// </remarks>
    public static IAgentToolsBuilder WithHttpContext(this IAgentToolsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHttpContextAccessor();

        return builder.WithContext<HttpAgentToolContextFactory>();
    }

    /// <summary>
    /// Gates tools on the authorization they declare, via <c>IAuthorizationService</c>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Requires <c>AddAuthorization</c>. If omitted, composing a registry whose tools declare
    /// authorization fails.
    /// </remarks>
    public static IAgentToolsBuilder WithAuthorization(this IAgentToolsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddScoped<AgentToolAuthorizationPolicy>();
        builder.Services.TryAddSingleton<AgentToolEnforcementMarker>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IAgentToolAuthorizationFilter, AuthorizationAgentToolFilter>());

        return builder;
    }
}
