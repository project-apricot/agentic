using System.Collections.Concurrent;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Reads tool labels.
/// </summary>
public static class AgentToolLabels
{
    /// <summary>
    /// An empty label set.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, object?> None = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Labels already read per tool type.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, object?>> Cache = new();

    /// <summary>
    /// Gets the labels declared by <see cref="AgentToolLabelAttribute"/> on a type.
    /// </summary>
    /// <param name="type">The tool type.</param>
    /// <returns>The labels, empty where none are declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <remarks>
    /// Read base first, so a derived type restating a name overrides it.
    /// </remarks>
    public static IReadOnlyDictionary<string, object?> ForType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Cache.GetOrAdd(type, Read);
    }

    /// <summary>
    /// Gets the labels declared on a method and its declaring class.
    /// </summary>
    /// <param name="method">The tool method.</param>
    /// <returns>The labels, empty where none are declared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is null.</exception>
    /// <remarks>
    /// Class first, so a method restating a name overrides it.
    /// </remarks>
    public static IReadOnlyDictionary<string, object?> ForMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var declared = method.GetCustomAttributes<AgentToolLabelAttribute>(inherit: false).ToList();

        var family = method.DeclaringType is { } declaring ? ForType(declaring) : None;

        if (declared.Count == 0)
        {
            return family;
        }

        var labels = new Dictionary<string, object?>(family, StringComparer.Ordinal);

        foreach (var attribute in declared)
        {
            labels[attribute.Name] = attribute.Value;
        }

        return labels;
    }

    /// <summary>
    /// Gets a label's value if present and of the expected type.
    /// </summary>
    /// <typeparam name="TValue">The expected value type.</typeparam>
    /// <param name="tool">The tool.</param>
    /// <param name="name">The label name.</param>
    /// <param name="value">The value, if found.</param>
    /// <returns>True where the label exists and its value is a <typeparamref name="TValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public static bool TryGetLabel<TValue>(this IAgentToolDeclaration tool, string name, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (tool.Labels.TryGetValue(name, out var found) && found is TValue typed)
        {
            value = typed;

            return true;
        }

        value = default;

        return false;
    }

    /// <summary>
    /// Checks whether a tool carries a label.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="name">The label name.</param>
    /// <returns>True where the label exists, whatever its value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public static bool HasLabel(this IAgentToolDeclaration tool, string name)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return tool.Labels.ContainsKey(name);
    }

    /// <summary>
    /// Reads the labels off a type chain.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The labels.</returns>
    private static IReadOnlyDictionary<string, object?> Read(Type type)
    {
        var chain = new List<Type>();

        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            chain.Add(current);
        }

        chain.Reverse();

        var labels = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var attribute in chain.SelectMany(entry => entry.GetCustomAttributes<AgentToolLabelAttribute>(inherit: false)))
        {
            labels[attribute.Name] = attribute.Value;
        }

        return labels.Count == 0 ? None : labels;
    }
}
