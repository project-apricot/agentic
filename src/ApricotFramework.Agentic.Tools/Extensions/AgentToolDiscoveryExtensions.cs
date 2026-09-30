using ApricotFramework.Agentic.Tools.Discovery;
using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Extensions;

/// <summary>
/// Registering whatever tools an assembly holds.
/// </summary>
/// <remarks>
/// <para>
/// An oversized tool surface makes a model worse at choosing between what is on it,
/// and scanning is how a surface grows without anyone deciding that it should.
/// </para>
/// <para>
/// Scan narrowly. An assembly that also holds tools written for a test is how a surface acquires
/// tools nobody meant to offer, and the predicate is there for that.
/// </para>
/// </remarks>
public static class AgentToolDiscoveryExtensions
{
    /// <summary>
    /// Adds tools by their types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolTypes">The tools to add.</param>
    /// <param name="configure">What else to say about each of them, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// Takes both kinds: a class that is a tool, and a class whose methods are tools.
    /// </remarks>
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
    /// <param name="assembly">The assembly to look in, or null for the one calling this.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <param name="configure">What else to say about each tool found, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Finds concrete, non-generic classes deriving from <see cref="AgentTool"/>, and classes
    /// carrying <see cref="AgentToolTypeAttribute"/>. A tool is found because of what it is, not
    /// because it carries a marker nobody remembers - a second thing to remember would be a
    /// second thing to forget, with the failure being a tool that silently is not there.
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
    /// <param name="assemblies">The assemblies to look in.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static IServiceCollection AddAgentToolsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        return services.AddAgentToolTypes(
            [.. AgentToolDiscovery.FromAssemblies(assemblies), .. AgentToolDiscovery.ToolTypesFromAssemblies(assemblies)]);
    }

    /// <summary>
    /// Adds every tool found in the assembly a type belongs to.
    /// </summary>
    /// <typeparam name="TMarker">Any type from the assembly to look at.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <param name="configure">What else to say about each tool found, or null for nothing.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Says which assembly by pointing at something in it, which survives a rename where a string
    /// would not.
    /// </remarks>
    public static IServiceCollection AddAgentToolsFromAssemblyContaining<TMarker>(
        this IServiceCollection services,
        Func<Type, bool>? predicate = null,
        Action<IAgentToolConventionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolsFromAssembly(typeof(TMarker).Assembly, predicate, configure);
    }
}
