using ApricotFramework.Agentic.Tools.Discovery;
using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Extensions;

/// <summary>
/// Registers the tools an assembly contains.
/// </summary>
/// <remarks>
/// Scan narrowly: large tool surfaces degrade model tool choice, and broad scans can pick up tools
/// not meant to be offered (e.g. test tools). Use the predicate to exclude them.
/// </remarks>
public static class AgentToolDiscoveryExtensions
{
    /// <summary>
    /// Adds tools by type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolTypes">The tool types: tool classes or classes with tool methods.</param>
    /// <param name="configure">Extra metadata for each tool, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static IServiceCollection AddAgentToolTypes(this IServiceCollection services, IEnumerable<Type> toolTypes, Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolTypes);

        foreach (var toolType in toolTypes)
        {
            if (AgentToolDiscovery.IsToolType(toolType))
            {
                services.AddAgentToolType(toolType, configure);
            }
            else
            {
                services.AddAgentTool(toolType, configure);
            }
        }

        return services;
    }

    /// <summary>
    /// Adds every tool found in an assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan, or null for the calling assembly.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <param name="configure">Extra metadata for each tool, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Finds concrete, non-generic <see cref="AgentTool"/> classes and classes marked with
    /// <see cref="AgentToolTypeAttribute"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAgentToolsFromAssembly(
        this IServiceCollection services,
        Assembly? assembly = null,
        Func<Type, bool>? predicate = null,
        Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var looking = assembly ?? Assembly.GetCallingAssembly();

        return services.AddAgentToolTypes(
            [
                .. AgentToolDiscovery.FromAssembly(looking, predicate),
                .. AgentToolDiscovery.ToolTypesFromAssembly(looking, predicate)
            ],
            configure);
    }

    /// <summary>
    /// Adds every tool found in several assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static IServiceCollection AddAgentToolsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        return services.AddAgentToolTypes(
            [.. AgentToolDiscovery.FromAssemblies(assemblies), .. AgentToolDiscovery.ToolTypesFromAssemblies(assemblies)]);
    }

    /// <summary>
    /// Adds every tool found in the assembly containing a type.
    /// </summary>
    /// <typeparam name="TMarker">Any type from the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <param name="configure">Extra metadata for each tool, or null.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddAgentToolsFromAssemblyContaining<TMarker>(
        this IServiceCollection services,
        Func<Type, bool>? predicate = null,
        Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolsFromAssembly(typeof(TMarker).Assembly, predicate, configure);
    }
}
