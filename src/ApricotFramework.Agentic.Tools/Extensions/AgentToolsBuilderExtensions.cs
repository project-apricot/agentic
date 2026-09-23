using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Extensions;

/// <summary>
/// The decisions where sequence is part of the meaning.
/// </summary>
/// <remarks>
/// Everything here either replaces what came before it or nests inside it. A chain is what makes
/// that legible: you cannot write these out of order, so there is no rule to remember about
/// which registration wins. Additive wiring - a tool, a source, a validator, a filter - is an
/// <c>Add</c> on the service collection instead.
/// </remarks>
public static class AgentToolsBuilderExtensions
{
    /// <summary>
    /// Says how this host decides who is asking.
    /// </summary>
    /// <typeparam name="TFactory">The factory to use.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Replaces whatever came before it. Two answers to "who is asking" is not a configuration,
    /// it is a bug - so the last one in the chain is the one that applies, and a host kind's
    /// default is simply the one the entry point put there first.
    /// </remarks>
    public static IAgentToolsBuilder WithContext<TFactory>(this IAgentToolsBuilder builder)
        where TFactory : class, IAgentToolContextFactory
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.RemoveAll<IAgentToolContextFactory>();
        builder.Services.AddSingleton<IAgentToolContextFactory, TFactory>();

        return builder;
    }

    /// <summary>
    /// Wraps the invoker in something of the host's own.
    /// </summary>
    /// <typeparam name="TInvoker">The wrapper. Its constructor takes the invoker it wraps, and whatever else it needs.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no invoker has been registered to wrap.</exception>
    /// <remarks>
    /// For what needs to know the caller: an audit record, a per-person budget. Each call wraps
    /// the last, so the one written last is the one a caller reaches first - which is the reason
    /// this is a chain rather than a loose registration.
    /// </remarks>
    public static IAgentToolsBuilder DecorateInvoker<TInvoker>(this IAgentToolsBuilder builder)
        where TInvoker : class, IAgentToolInvoker
    {
        ArgumentNullException.ThrowIfNull(builder);

        Decorate<IAgentToolInvoker, TInvoker>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Wraps the executor in something of the host's own.
    /// </summary>
    /// <typeparam name="TExecutor">The wrapper. Its constructor takes the executor it wraps, and whatever else it needs.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no executor has been registered to wrap.</exception>
    /// <remarks>
    /// For what is about the surface rather than the caller: a rate limit, a span, a cap on how
    /// many tools a listing offers.
    /// </remarks>
    public static IAgentToolsBuilder DecorateExecutor<TExecutor>(this IAgentToolsBuilder builder)
        where TExecutor : class, IAgentToolExecutor
    {
        ArgumentNullException.ThrowIfNull(builder);

        Decorate<IAgentToolExecutor, TExecutor>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Puts something of the host's own around a service registered behind an interface.
    /// </summary>
    /// <typeparam name="TService">The interface.</typeparam>
    /// <typeparam name="TWrapper">The wrapper.</typeparam>
    /// <param name="services">The service collection.</param>
    private static void Decorate<TService, TWrapper>(IServiceCollection services)
        where TService : class
        where TWrapper : class, TService
    {
        var wrapped = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(TService))
                      ?? throw new InvalidOperationException(
                          $"No {typeof(TService).Name} is registered to wrap. Call AddAgentToolsCore or a host kind's entry point first.");

        services.Remove(wrapped);

        services.Add(ServiceDescriptor.Describe(
            typeof(TService),
            provider => ActivatorUtilities.CreateInstance<TWrapper>(provider, Resolve(provider, wrapped)),
            wrapped.Lifetime));
    }

    /// <summary>
    /// Builds whatever a captured descriptor describes.
    /// </summary>
    /// <param name="provider">Where the descriptor's own dependencies come from.</param>
    /// <param name="descriptor">The descriptor to build.</param>
    /// <returns>The instance.</returns>
    /// <remarks>
    /// A descriptor can say what it provides in three ways, and a wrapper has to cope with all of
    /// them, or it works until somebody registers theirs differently.
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
