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
/// Registers tools and the services that compose and run them.
/// </summary>
/// <remarks>No flags: registering something is what makes it apply.</remarks>
public static class AgentToolServiceCollectionExtensions
{
    /// <summary>
    /// Adds the registry, invoker and executor, and nothing opinionated.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for order-sensitive configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Uses <c>TryAdd</c>, so existing host registrations win. Also registers the tripwire that refuses
    /// to compose a tool declaring a gate nothing enforces.
    /// </remarks>
    public static IAgentToolsBuilder AddAgentToolsCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolSource, RegistrationAgentToolSource>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentToolValidator, EnforcementDeclaredValidator>());
        services.TryAddSingleton<IAgentToolRegistry, AgentToolRegistry>();
        services.TryAddScoped<IAgentToolInvoker, AgentToolInvoker>();
        services.TryAddSingleton<IAgentToolExecutor, AgentToolExecutor>();
        services.TryAddSingleton<IAgentToolContextFactory, DefaultAgentToolContextFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AgentToolStartupValidation>());

        return new AgentToolsBuilder(services);
    }

    /// <summary>
    /// Adds one tool written as a class.
    /// </summary>
    /// <typeparam name="TTool">The tool type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Extra metadata, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Registered as scoped and resolved per call. Attributes on the type and its bases become its
    /// metadata, so e.g. an authorization attribute becomes a gate automatically.
    /// </remarks>
    public static IServiceCollection AddAgentTool<TTool>(this IServiceCollection services, Action<IAgentToolConventionBuilder>? configure = null)
        where TTool : AgentTool
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentTool(typeof(TTool), configure);
    }

    /// <summary>
    /// Adds one tool written as a class, by type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolType">The tool type.</param>
    /// <param name="configure">Extra metadata, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="toolType"/> is not a concrete tool.</exception>
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
    /// <typeparam name="TToolType">The class declaring the methods.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Extra metadata for each tool, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// The class is resolved per call; method parameters can also be bound from the call's scope.
    /// </remarks>
    public static IServiceCollection AddAgentToolType<TToolType>(this IServiceCollection services, Action<IAgentToolConventionBuilder>? configure = null)
        where TToolType : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolType(typeof(TToolType), configure);
    }

    /// <summary>
    /// Adds every tool method on a class, by type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolType">The class declaring the methods.</param>
    /// <param name="configure">Extra metadata for each tool, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">The type declares no tool methods.</exception>
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
    /// <param name="declaration">What the function cannot declare itself.</param>
    /// <param name="configure">Extra metadata, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <remarks>
    /// The function is registered unwrapped, so consumers can still reach what is behind it (e.g. an
    /// <c>McpClientTool</c>). It has no attributes, so declare any gate in <paramref name="configure"/>;
    /// otherwise an authorization filter refuses the tool.
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
    /// Adds a tool source.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddAgentToolSource<TSource>(this IServiceCollection services)
        where TSource : class, IAgentToolSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentToolSource, TSource>();

        return services;
    }

    /// <summary>
    /// Adds a tool source built by a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="source">Builds the source.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <remarks>Needed for sources that wrap another, such as a curated source.</remarks>
    public static IServiceCollection AddAgentToolSource(this IServiceCollection services, Func<IServiceProvider, IAgentToolSource> source)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(source);

        services.AddSingleton(source);

        return services;
    }

    /// <summary>
    /// Adds a validator applied to every declaration as the registry composes.
    /// </summary>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddAgentToolValidator<TValidator>(this IServiceCollection services)
        where TValidator : class, IAgentToolValidator
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentToolValidator, TValidator>();

        return services;
    }

    /// <summary>
    /// Adds a filter deciding whether a caller can see a tool.
    /// </summary>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime; scoped by default.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// For scope, not permission (use <see cref="AddAgentToolAuthorizationFilter{TFilter}"/>). Scoped by
    /// default because filters usually depend on scoped services; use a longer lifetime only if it depends
    /// on nothing scoped, or it will capture the first caller's services.
    /// </remarks>
    public static IServiceCollection AddAgentToolFilter<TFilter>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TFilter : class, IAgentToolFilter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(ServiceDescriptor.Describe(typeof(IAgentToolFilter), typeof(TFilter), lifetime));

        return services;
    }

    /// <summary>
    /// Adds an authorization filter deciding whether a caller may use a visible tool.
    /// </summary>
    /// <typeparam name="TFilter">The authorization filter type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime; scoped by default.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// For custom permission checks; the attribute-based one is <c>WithAuthorization()</c> in the ASP.NET
    /// Core package. Registering one satisfies the enforcement tripwire.
    /// </remarks>
    public static IServiceCollection AddAgentToolAuthorizationFilter<TFilter>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TFilter : class, IAgentToolAuthorizationFilter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(ServiceDescriptor.Describe(typeof(IAgentToolAuthorizationFilter), typeof(TFilter), lifetime));
        services.TryAddSingleton<AgentToolEnforcementMarker>();

        return services;
    }

    /// <summary>
    /// Adds an exception translator.
    /// </summary>
    /// <typeparam name="TTranslator">The translator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The service lifetime; scoped by default.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>Translators are asked in registration order; the first match wins.</remarks>
    public static IServiceCollection AddAgentToolExceptionTranslator<TTranslator>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TTranslator : class, IAgentToolExceptionTranslator
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(ServiceDescriptor.Describe(typeof(IAgentToolExceptionTranslator), typeof(TTranslator), lifetime));

        return services;
    }

    /// <summary>
    /// Records a deferred descriptor factory for one tool.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="describe">Builds the descriptor.</param>
    /// <returns>The same collection.</returns>
    private static IServiceCollection AddAgentToolDescriptor(this IServiceCollection services, Func<IServiceProvider, AgentToolDescriptor> describe)
    {
        services.Configure<AgentToolRegistrationOptions>(options => options.Registrations.Add(describe));

        return services;
    }
}
