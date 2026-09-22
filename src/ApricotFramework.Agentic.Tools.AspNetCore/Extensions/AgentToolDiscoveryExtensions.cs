using ApricotFramework.Agentic.Tools.Discovery;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Extensions;

/// <summary>
/// Registers tools found by scanning rather than named one at a time.
/// </summary>
/// <remarks>
/// <para>
/// For the host with a convention and enough tools that listing them becomes its own maintenance
/// problem. A host with a handful is better off naming them: the list is then the surface, and
/// somebody has to decide to add to it.
/// </para>
/// <para>
/// None of these return an <see cref="IAgentToolBuilder"/>, because one builder cannot speak for
/// however many tools were found. A requirement that ought to apply to a whole family is better
/// said with an authorization attribute on a shared base class: attributes are collected with
/// inheritance, so it reaches every tool deriving from it without anything registered per tool.
/// </para>
/// </remarks>
public static class AgentToolDiscoveryExtensions
{
    /// <summary>
    /// Adds tools by their types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolTypes">The tools to add.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when one of the types is not a tool that can be instantiated as one.</exception>
    public static IServiceCollection AddAgentToolTypes(this IServiceCollection services, IEnumerable<Type> toolTypes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(toolTypes);

        foreach (var toolType in toolTypes)
        {
            services.AddAgentTool(toolType);
        }

        return services;
    }

    /// <summary>
    /// Adds every tool found in an assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to look in, or null for the one calling this.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Finds concrete, non-generic types deriving from <see cref="AgentTool"/> and not carrying
    /// <see cref="AgentToolIgnoreAttribute"/>, public or otherwise. Scanning an assembly that also
    /// holds tools written for a test is how a surface acquires tools nobody meant to offer, so
    /// the predicate is there for a host that needs to be narrower than an assembly.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAgentToolsFromAssembly(this IServiceCollection services, Assembly? assembly = null, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolTypes(AgentToolDiscovery.FromAssembly(assembly ?? Assembly.GetCallingAssembly(), predicate));
    }

    /// <summary>
    /// Adds every tool found in several assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to look in.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IServiceCollection AddAgentToolsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        return services.AddAgentToolTypes(AgentToolDiscovery.FromAssemblies(assemblies));
    }

    /// <summary>
    /// Adds every tool found in the assembly a type belongs to.
    /// </summary>
    /// <typeparam name="TMarker">Any type from the assembly to look at.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Says which assembly by pointing at something in it, which survives a rename where a string
    /// would not.
    /// </remarks>
    public static IServiceCollection AddAgentToolsFromAssemblyContaining<TMarker>(this IServiceCollection services, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolsFromAssembly(typeof(TMarker).Assembly, predicate);
    }
}
