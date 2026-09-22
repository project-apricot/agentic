using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Builders;
using ApricotFramework.Agentic.Tools.AspNetCore.Validators;
using ApricotFramework.Agentic.Tools.Discovery;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Options;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Registers agent tools and what runs them.
/// </summary>
public static class AgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry, the invoker, and authorization read from what a tool's registration carries.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// There is nothing to configure. Registering a filter is what makes it apply, and this
    /// registers none - not even authorization, which is <see cref="AddAgentToolAuthorization"/>
    /// and deliberately a separate call. A host that has not configured ASP.NET Core
    /// authorization at all should not be handed a filter that cannot resolve
    /// <c>IAuthorizationService</c>, and a host that wants no authorization should be able to say
    /// so by not asking rather than by unpicking a registration.
    /// </para>
    /// <para>
    /// What this does register is <see cref="AuthorizationEnforcedValidator"/>, so forgetting the
    /// other call is caught rather than silently permitted.
    /// </para>
    /// <para>
    /// No declaration checks are registered. What a tool ought to carry beyond a name is the
    /// host's judgment - see <c>ApricotFramework.Agentic.Tools.Validators</c> for the ones this
    /// library ships, and add the ones you want.
    /// </para>
    /// <para>
    /// Everything is registered with <c>TryAdd</c>, so a host that has already registered one of
    /// its own keeps it.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolValidator, AuthorizationEnforcedValidator>());

        // the tools registered in code, turned into descriptors at once. the implementation type is
        // named rather than inferred, because TryAddEnumerable dedupes on it, and a factory that
        // does not name one is indistinguishable from any other
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolSource, StaticAgentToolSource>(
            provider => new StaticAgentToolSource(
                provider.GetServices<AgentToolRegistration>().Select(registration => registration.Build(provider)))));

        services.TryAddSingleton<IAgentToolRegistry, AgentToolRegistry>();

        // behind the interface only. a host resolving the concrete type would reach past anything
        // wrapped around it, and a rate limit or an audit log nobody reaches is worse than none
        services.TryAddSingleton<IAgentToolInvoker, AgentToolInvoker>();

        return services;
    }

    /// <summary>
    /// Adds authorization: the attributes a tool declares, decided through <c>IAuthorizationService</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Separate from <see cref="AddAgentTools"/> on purpose, and the same shape the MCP SDK
    /// settled on. A host with tools and no authorization is a real configuration, and one that
    /// has not called <c>AddAuthorization</c> would otherwise get an unresolvable
    /// <c>IAuthorizationService</c> instead of an answer.
    /// </para>
    /// <para>
    /// Forgetting it is caught rather than permitted: <see cref="AuthorizationEnforcedValidator"/>
    /// refuses to compose a registry whose tools declare authorization nothing is enforcing.
    /// </para>
    /// <para>
    /// Expects <c>AddAuthorization</c> to have been called, since that is where the policies a
    /// tool names come from.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentToolAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<AgentToolAuthorizationPolicy>();
        services.TryAddSingleton<AgentToolAuthorizationMarker>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolFilter, AuthorizationAgentToolFilter>());

        return services;
    }

    /// <summary>
    /// Adds one tool.
    /// </summary>
    /// <typeparam name="TTool">The tool to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The tool, so more can be said about it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// The attributes on <typeparamref name="TTool"/> and everything it derives from are collected
    /// as the tool's metadata, which is how an authorization attribute on the class becomes a gate
    /// without anyone wiring it up.
    /// </remarks>
    public static IAgentToolBuilder AddAgentTool<TTool>(this IServiceCollection services)
        where TTool : AgentTool
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.Register(
            new AgentToolRegistration(provider => ActivatorUtilities.CreateInstance<TTool>(provider))
                .WithAttributesOf(typeof(TTool)));
    }

    /// <summary>
    /// Adds one tool by its type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolType">The tool to add.</param>
    /// <returns>The tool, so more can be said about it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolType"/> is not a tool that can be instantiated as one.</exception>
    public static IAgentToolBuilder AddAgentTool(this IServiceCollection services, Type toolType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolType);

        if (!AgentToolDiscovery.IsTool(toolType))
        {
            throw new ArgumentException($"'{toolType.FullName}' is not a concrete {nameof(AgentTool)}.", nameof(toolType));
        }

        return services.Register(
            new AgentToolRegistration(provider => (AgentTool)ActivatorUtilities.CreateInstance(provider, toolType))
                .WithAttributesOf(toolType));
    }

    /// <summary>
    /// Adds one tool that has already been built.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="tool">The tool to add.</param>
    /// <returns>The tool, so more can be said about it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IAgentToolBuilder AddAgentTool(this IServiceCollection services, AgentTool tool)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(tool);

        return services.Register(new AgentToolRegistration(_ => tool).WithAttributesOf(tool.GetType()));
    }

    /// <summary>
    /// Adds one tool backed by an <see cref="AIFunction"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="function">The function behind the tool.</param>
    /// <param name="options">What the function cannot say for itself.</param>
    /// <returns>The tool, so more can be said about it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// A function has no type to carry an attribute, so this is the registration that most often
    /// wants <c>RequireAuthorization</c> after it - and without one, the authorization filter
    /// refuses the tool rather than opening it.
    /// </remarks>
    public static IAgentToolBuilder AddAgentTool(this IServiceCollection services, AIFunction function, AgentToolCreateOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentTool(AgentTool.Create(function, options));
    }

    /// <summary>
    /// Adds somewhere else tools come from.
    /// </summary>
    /// <typeparam name="TSource">The source to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddAgentToolSource<TSource>(this IServiceCollection services)
        where TSource : class, IAgentToolSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentToolSource, TSource>();

        return services;
    }

    /// <summary>
    /// Adds somewhere else tools come from, built by hand.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="source">Builds the source.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// The overload a curated source needs, since wrapping one source in another cannot be
    /// expressed by naming a type. A source hands over descriptors, so whatever should gate its
    /// tools is attached there - see <c>AgentToolCuration.Adding</c>.
    /// </remarks>
    public static IServiceCollection AddAgentToolSource(this IServiceCollection services, Func<IServiceProvider, IAgentToolSource> source)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(source);

        services.AddSingleton(source);

        return services;
    }

    /// <summary>
    /// Adds a check applied to every registration while the registry is being built.
    /// </summary>
    /// <typeparam name="TValidator">The check to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Where a host puts the rules, this library cannot hold an opinion about - a naming
    /// convention, a closed set of labels, a description on every tool.
    /// </remarks>
    public static IServiceCollection AddAgentToolValidator<TValidator>(this IServiceCollection services)
        where TValidator : class, IAgentToolValidator
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentToolValidator, TValidator>();

        return services;
    }

    /// <summary>
    /// Adds something that decides whether a caller may reach a tool.
    /// </summary>
    /// <typeparam name="TFilter">The filter to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Applies to the listing and the call alike. Registering it is what makes it apply.
    /// </remarks>
    public static IServiceCollection AddAgentToolFilter<TFilter>(this IServiceCollection services)
        where TFilter : class, IAgentToolFilter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentToolFilter, TFilter>();

        return services;
    }

    /// <summary>
    /// Wraps the invoker in something of the host's own.
    /// </summary>
    /// <typeparam name="TInvoker">The wrapper. Its constructor takes the invoker it wraps, and whatever else it needs from the container.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no invoker has been registered to wrap.</exception>
    /// <remarks>
    /// <para>
    /// For a log line, a span, a rate limit, an approval, or a budget - anything about the call
    /// rather than about what exists. Derive the wrapper from <c>DelegatingAgentToolInvoker</c> and
    /// it needs to say only what it changes.
    /// </para>
    /// <para>
    /// Applied in the order called, each wrapping is the last, so the one registered last is the one
    /// a caller reaches first.
    /// </para>
    /// </remarks>
    public static IServiceCollection DecorateAgentToolInvoker<TInvoker>(this IServiceCollection services)
        where TInvoker : class, IAgentToolInvoker
    {
        ArgumentNullException.ThrowIfNull(services);

        var wrapped = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAgentToolInvoker))
                      ?? throw new InvalidOperationException($"No {nameof(IAgentToolInvoker)} is registered to wrap. Call {nameof(AddAgentTools)} first.");

        services.Remove(wrapped);

        services.Add(ServiceDescriptor.Describe(
            typeof(IAgentToolInvoker),
            provider => ActivatorUtilities.CreateInstance<TInvoker>(provider, Resolve(provider, wrapped)),
            wrapped.Lifetime));

        return services;
    }

    /// <summary>
    /// Records a registration, which is also the builder over it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registration">The registration.</param>
    /// <returns>The registration.</returns>
    private static AgentToolRegistration Register(this IServiceCollection services, AgentToolRegistration registration)
    {
        services.AddSingleton(registration);

        return registration;
    }

    /// <summary>
    /// Builds whatever a captured descriptor describes.
    /// </summary>
    /// <param name="provider">Where the descriptor's own dependencies come from.</param>
    /// <param name="descriptor">The descriptor to build.</param>
    /// <returns>The instance.</returns>
    /// <remarks>
    /// A descriptor can say what it provides in three ways, and a wrapper has to cope with all of
    /// them, or it works until somebody registers their invoker differently.
    /// </remarks>
    private static object Resolve(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        return descriptor.ImplementationFactory is not null
            ? descriptor.ImplementationFactory(provider)
            : ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
    }
}
