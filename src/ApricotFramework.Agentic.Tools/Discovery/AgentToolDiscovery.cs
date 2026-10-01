using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Discovery;

/// <summary>
/// Finds tools in assemblies by reflection.
/// </summary>
/// <remarks>
/// Discovery is by derivation from <see cref="AgentTool"/>, not a marker attribute, so a tool cannot
/// be silently missed. Exclude types with <see cref="AgentToolIgnoreAttribute"/> or the predicate.
/// </remarks>
public static class AgentToolDiscovery
{
    /// <summary>
    /// Finds the tool types in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <returns>The tool types, unordered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
    /// <remarks>
    /// Includes non-public types. Excludes abstract types, open generics and types marked with
    /// <see cref="AgentToolIgnoreAttribute"/>.
    /// </remarks>
    public static IEnumerable<Type> FromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes().Where(type => IsTool(type) && (predicate?.Invoke(type) ?? true));
    }

    /// <summary>
    /// Finds the tool types in several assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <returns>The distinct tool types, unordered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies"/> is null.</exception>
    public static IEnumerable<Type> FromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies.SelectMany(assembly => FromAssembly(assembly, predicate)).Distinct();
    }

    /// <summary>
    /// Finds the classes declaring tool methods in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <returns>The types, unordered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
    /// <remarks>
    /// Matches classes marked with <see cref="AgentToolTypeAttribute"/>, whose methods carry
    /// <see cref="AgentToolAttribute"/>.
    /// </remarks>
    public static IEnumerable<Type> ToolTypesFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes().Where(type => IsToolType(type) && (predicate?.Invoke(type) ?? true));
    }

    /// <summary>
    /// Finds the classes declaring tool methods in several assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="predicate">An extra filter on candidate types, or null.</param>
    /// <returns>The distinct types, unordered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies"/> is null.</exception>
    public static IEnumerable<Type> ToolTypesFromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies.SelectMany(assembly => ToolTypesFromAssembly(assembly, predicate)).Distinct();
    }

    /// <summary>
    /// Checks whether a type is a discoverable class declaring tool methods.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is.</returns>
    public static bool IsToolType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               && type.GetCustomAttribute<AgentToolTypeAttribute>(inherit: false) is not null
               && type.GetCustomAttribute<AgentToolIgnoreAttribute>(inherit: false) is null;
    }

    /// <summary>
    /// Checks whether a type is a discoverable, instantiable tool.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is.</returns>
    public static bool IsTool(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               && typeof(AgentTool).IsAssignableFrom(type)
               && type.GetCustomAttribute<AgentToolIgnoreAttribute>(inherit: false) is null;
    }
}
