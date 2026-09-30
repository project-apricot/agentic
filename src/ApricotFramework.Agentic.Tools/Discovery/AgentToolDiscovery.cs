using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Discovery;

/// <summary>
/// Finds tools by looking for them.
/// </summary>
/// <remarks>
/// <para>
/// A tool is discovered because it derives from <see cref="AgentTool"/>, not because it carries a
/// marker attribute. Deriving is already the declaration, and a second thing to remember would be
/// a second thing to forget - with the failure being a tool that silently is not there.
/// </para>
/// <para>
/// The risk that runs the other way - something swept up that should not have been - is mostly
/// caught downstream: a type picked up by accident still has to satisfy the validators, and one
/// that was never meant to be offered rarely declares a description or an authorization. Where that
/// is not enough, <see cref="AgentToolIgnoreAttribute"/> and the predicate are.
/// </para>
/// </remarks>
public static class AgentToolDiscovery
{
    /// <summary>
    /// Finds the tool types in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to look in.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The tool types found, in no particular order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null.</exception>
    /// <remarks>
    /// Includes types that are not public, since a host may reasonably keep its tools internal.
    /// Excludes anything that cannot be instantiated as one tool: an abstract base, an open
    /// generic, or a type carrying <see cref="AgentToolIgnoreAttribute"/>.
    /// </remarks>
    public static IEnumerable<Type> FromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes().Where(type => IsTool(type) && (predicate?.Invoke(type) ?? true));
    }

    /// <summary>
    /// Finds the tool types in several assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to look in.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The tool types found, each once, in no particular order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is null.</exception>
    public static IEnumerable<Type> FromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies.SelectMany(assembly => FromAssembly(assembly, predicate)).Distinct();
    }

    /// <summary>
    /// Finds the types holding tool methods in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to look in.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The types found, in no particular order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null.</exception>
    /// <remarks>
    /// The other way a tool is written: a class carrying <see cref="AgentToolTypeAttribute"/>,
    /// whose methods carry <see cref="AgentToolAttribute"/>.
    /// </remarks>
    public static IEnumerable<Type> ToolTypesFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes().Where(type => IsToolType(type) && (predicate?.Invoke(type) ?? true));
    }

    /// <summary>
    /// Finds the types holding tool methods in several assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to look in.</param>
    /// <param name="predicate">A further test each candidate has to pass, or null for none.</param>
    /// <returns>The types found, each once, in no particular order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is null.</exception>
    public static IEnumerable<Type> ToolTypesFromAssemblies(IEnumerable<Assembly> assemblies, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies.SelectMany(assembly => ToolTypesFromAssembly(assembly, predicate)).Distinct();
    }

    /// <summary>
    /// Checks whether a type holds tool methods.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True where the type is a discoverable holder of tool methods.</returns>
    public static bool IsToolType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               && type.GetCustomAttribute<AgentToolTypeAttribute>(inherit: false) is not null
               && type.GetCustomAttribute<AgentToolIgnoreAttribute>(inherit: false) is null;
    }

    /// <summary>
    /// Checks whether a type is a tool that can be instantiated as one.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True where the type is a discoverable tool.</returns>
    public static bool IsTool(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               && typeof(AgentTool).IsAssignableFrom(type)
               && type.GetCustomAttribute<AgentToolIgnoreAttribute>(inherit: false) is null;
    }
}
