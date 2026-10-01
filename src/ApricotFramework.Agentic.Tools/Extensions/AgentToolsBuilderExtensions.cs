using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.Extensions;

/// <summary>
/// Order-sensitive configuration of the tool pipeline.
/// </summary>
/// <remarks>
/// Each call replaces or wraps what came before. Order-free registrations are <c>Add</c> methods on
/// the service collection.
/// </remarks>
public static class AgentToolsBuilderExtensions
{
    /// <summary>
    /// Sets the context factory that decides who is asking.
    /// </summary>
    /// <typeparam name="TFactory">The factory type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <remarks>
    /// Replaces any earlier factory; the last call wins. Registered scoped, so it may depend on scoped services.
    /// </remarks>
    public static IAgentToolsBuilder WithContext<TFactory>(this IAgentToolsBuilder builder)
        where TFactory : class, IAgentToolContextFactory
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.RemoveAll<IAgentToolContextFactory>();
        builder.Services.AddScoped<IAgentToolContextFactory, TFactory>();

        return builder;
    }

    /// <summary>
    /// Wraps the invoker in a decorator.
    /// </summary>
    /// <typeparam name="TInvoker">The decorator; its constructor takes the wrapped invoker.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No invoker is registered to wrap.</exception>
    /// <remarks>
    /// For concerns needing the caller (auditing, per-person budgets). The last decorator added runs first.
    /// </remarks>
    public static IAgentToolsBuilder DecorateInvoker<TInvoker>(this IAgentToolsBuilder builder)
        where TInvoker : class, IAgentToolInvoker
    {
        ArgumentNullException.ThrowIfNull(builder);

        Decorate<IAgentToolInvoker, TInvoker>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Wraps the executor in a decorator.
    /// </summary>
    /// <typeparam name="TExecutor">The decorator; its constructor takes the wrapped executor.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No executor is registered to wrap.</exception>
    /// <remarks>For surface-level concerns (rate limits, tracing, listing caps).</remarks>
    public static IAgentToolsBuilder DecorateExecutor<TExecutor>(this IAgentToolsBuilder builder)
        where TExecutor : class, IAgentToolExecutor
    {
        ArgumentNullException.ThrowIfNull(builder);

        Decorate<IAgentToolExecutor, TExecutor>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Replaces the last registration of a service with a decorator around it.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TWrapper">The decorator type.</typeparam>
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
    /// Creates the instance a captured descriptor describes.
    /// </summary>
    /// <param name="provider">Supplies the descriptor's dependencies.</param>
    /// <param name="descriptor">The descriptor.</param>
    /// <returns>The instance.</returns>
    /// <remarks>Handles instance, factory and type descriptors alike.</remarks>
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
