using System.Collections.Concurrent;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Reads the labels a tool declares and reads them back out again.
/// </summary>
public static class AgentToolLabels
{
    /// <summary>
    /// An absence of labels.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, object?> None = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// What has already been read for a tool type?
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, object?>> Cache = new();

    /// <summary>
    /// Gets the labels declared by <see cref="AgentToolLabelAttribute"/> on a type.
    /// </summary>
    /// <param name="type">The tool type to read.</param>
    /// <returns>The labels, empty where the type declares none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    /// <remarks>
    /// Read from the whole type chain, base first, so a derived tool restating a name overrides
    /// the family rather than colliding with it.
    /// </remarks>
    public static IReadOnlyDictionary<string, object?> ForType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Cache.GetOrAdd(type, Read);
    }

    /// <summary>
    /// Gets a label's value, where the tool carries one of those names and that type.
    /// </summary>
    /// <typeparam name="TValue">The type the value is expected to be.</typeparam>
    /// <param name="tool">The tool to read.</param>
    /// <param name="name">The label to read.</param>
    /// <param name="value">The value, where there was one.</param>
    /// <returns>True where the tool carries the label and its value is a <typeparamref name="TValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public static bool TryGetLabel<TValue>(this AgentTool tool, string name, out TValue? value)
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
    /// <param name="tool">The tool to read.</param>
    /// <param name="name">The label to look for.</param>
    /// <returns>True, where the tool carries it, whatever its value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public static bool HasLabel(this AgentTool tool, string name)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return tool.Labels.ContainsKey(name);
    }

    /// <summary>
    /// Reads the labels off a type chain.
    /// </summary>
    /// <param name="type">The type to read.</param>
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
