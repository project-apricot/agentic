using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registration;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Validators;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApricotFramework.Agentic.Tools.Extensions;

/// <summary>
/// Registering tools, and the things that compose and run them.
/// </summary>
/// <remarks>
/// Nothing here is an option or a flag. Registering something is what makes it apply, and a host
/// that wants none of a thing says so by not asking rather than by unpicking a registration.
/// </remarks>
public static class AgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry, the invoker and the executor, and nothing opinionated.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder carrying the decisions where sequence is part of the meaning.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// No filters are registered - not even authorization, which lives in the ASP.NET Core
    /// package and is a separate call there. No declaration checks either: what a tool ought to
    /// carry beyond a name is the host's judgment, and the ones this library ships wait to be
    /// chosen.
    /// </para>
    /// <para>
    /// The one thing registered without being asked for is the tripwire: a tool declaring a gate
    /// that nothing enforces refuses to compose.
    /// </para>
    /// <para>
    /// Everything is registered with <c>TryAdd</c>, so a host that has already registered one of
    /// its own keeps it.
    /// </para>
    /// </remarks>
    public static IAgentToolsBuilder AddAgentToolsCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        // the implementation type is named rather than produced by a factory, because
        // TryAddEnumerable dedupes on it and a factory that names none is indistinguishable from
        // any other source a host registers
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolSource, RegistrationAgentToolSource>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolValidator, EnforcementDeclaredValidator>());

        services.TryAddSingleton<IAgentToolRegistry, AgentToolRegistry>();

        // behind the interfaces only. a host resolving a concrete type would reach past anything
        // wrapped around it, and a rate limit or an audit log nobody reaches is worse than none
        services.TryAddSingleton<IAgentToolInvoker, AgentToolInvoker>();
        services.TryAddSingleton<IAgentToolExecutor, AgentToolExecutor>();

        services.TryAddSingleton<IAgentToolContextFactory, DefaultAgentToolContextFactory>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AgentToolStartupValidation>());

        return new AgentToolsBuilder(services);
    }

    /// <summary>
    /// Adds one tool written as a class.
    /// </summary>
    /// <typeparam name="TTool">The tool to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">What else to say about it, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// The type is registered as scoped and resolved again for every call, so it takes its
    /// dependencies through its constructor the way an endpoint does. The attributes on it and on
    /// everything it derives from become its metadata, which is how an authorization attribute on
    /// the class becomes a gate without anyone wiring it up.
    /// </remarks>
    public static IServiceCollection AddAgentTool<TTool>(this IServiceCollection services, Action<IAgentToolConventionBuilder>? configure = null)
        where TTool : AgentTool
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentTool(typeof(TTool), configure);
    }

    /// <summary>
    /// Adds one tool written as a class, by its type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolType">The tool to add.</param>
    /// <param name="configure">What else to say about it, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolType"/> is not a tool that can be instantiated as one.</exception>
    public static IServiceCollection AddAgentTool(this IServiceCollection services, Type toolType, Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolType);

        if (!Discovery.AgentToolDiscovery.IsTool(toolType))
        {
            throw new ArgumentException($"'{toolType.FullName}' is not a concrete {nameof(AgentTool)}.", nameof(toolType));
        }

        services.TryAddScoped(toolType);

        var metadata = new AgentToolConventionBuilder(services).WithAttributesOf(toolType).Build(configure);

        return services.AddAgentToolDescriptor(provider => AgentToolTypes.Describe(provider, toolType, metadata));
    }

    /// <summary>
    /// Adds every tool method on a class.
    /// </summary>
    /// <typeparam name="TToolType">The class holding the methods.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">What else to say about each of them, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// The other way to write a tool. The class is resolved per call exactly as a class-per-tool
    /// is, so a method takes what it needs through the class's constructor - or through its own
    /// parameters, which the function factory binds from the same scope.
    /// </remarks>
    public static IServiceCollection AddAgentToolType<TToolType>(this IServiceCollection services, Action<IAgentToolConventionBuilder>? configure = null)
        where TToolType : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolType(typeof(TToolType), configure);
    }

    /// <summary>
    /// Adds every tool method on a class, by its type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolType">The class holding the methods.</param>
    /// <param name="configure">What else to say about each of them, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the type declares no tool methods.</exception>
    public static IServiceCollection AddAgentToolType(this IServiceCollection services, Type toolType, Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolType);

        var methods = AgentToolMethods.In(toolType).ToList();

        if (methods.Count == 0)
        {
            throw new ArgumentException($"'{toolType.FullName}' declares no methods carrying {nameof(AgentToolAttribute)}.", nameof(toolType));
        }

        services.TryAddScoped(toolType);

        foreach (var method in methods)
        {
            var metadata = new AgentToolConventionBuilder(services).WithAttributesOf(method).Build(configure);

            services.AddAgentToolDescriptor(_ => AgentToolMethods.Describe(toolType, method, metadata));
        }

        return services;
    }

    /// <summary>
    /// Adds one tool that is already a function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="function">The function.</param>
    /// <param name="declaration">What the function cannot say for itself.</param>
    /// <param name="configure">What else to say about it, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// <para>
    /// For a delegate, something built from configuration, or a tool read off a server this host
    /// merely speaks to. Nothing wraps it: the function is registered as it is, so a consumer
    /// reaching through it - for the <c>McpClientTool</c> behind it, say - still finds what is
    /// there.
    /// </para>
    /// <para>
    /// A function has no class to carry an attribute, so this is the registration that most often
    /// wants something said in the callback - and without it, an authorization filter refuses the
    /// tool rather than opening it.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentTool(
        this IServiceCollection services,
        AIFunctionDeclaration function,
        IAgentToolDeclaration declaration,
        Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(declaration);

        var metadata = new AgentToolConventionBuilder(services).Build(configure);

        return services.AddAgentToolDescriptor(_ => new AgentToolDescriptor(function, declaration, metadata));
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
    /// expressed by naming a type.
    /// </remarks>
    public static IServiceCollection AddAgentToolSource(this IServiceCollection services, Func<IServiceProvider, IAgentToolSource> source)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(source);

        services.AddSingleton(source);

        return services;
    }

    /// <summary>
    /// Adds a check applied to every declaration as the registry composes.
    /// </summary>
    /// <typeparam name="TValidator">The check to add.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
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
    /// <param name="lifetime">How long one lives, defaulting to the life of the process.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Applies to the listing and the call alike. Scoped is available because the executor
    /// resolves the invoker from the call's own scope, so a filter reading per-call state works.
    /// </remarks>
    public static IServiceCollection AddAgentToolFilter<TFilter>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TFilter : class, IAgentToolFilter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(ServiceDescriptor.Describe(typeof(IAgentToolFilter), typeof(TFilter), lifetime));

        return services;
    }

    /// <summary>
    /// Records how to describe one tool, once there is a container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="describe">How to describe it.</param>
    /// <returns>The same collection, for chaining.</returns>
    private static IServiceCollection AddAgentToolDescriptor(this IServiceCollection services, Func<IServiceProvider, AgentToolDescriptor> describe)
    {
        services.Configure<AgentToolRegistrationOptions>(options => options.Registrations.Add(describe));

        return services;
    }
}
