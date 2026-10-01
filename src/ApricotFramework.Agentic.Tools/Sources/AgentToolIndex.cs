using System.Collections.Frozen;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// Name lookup for fixed tool lists.
/// </summary>
/// <remarks>The first tool under a name wins, matching the registry.</remarks>
internal static class AgentToolIndex
{
    /// <summary>
    /// Indexes tools by name.
    /// </summary>
    /// <param name="tools">The tools.</param>
    /// <returns>The index.</returns>
    internal static FrozenDictionary<string, AgentToolDescriptor> By(IEnumerable<AgentToolDescriptor> tools)
    {
        var index = new Dictionary<string, AgentToolDescriptor>(StringComparer.Ordinal);

        foreach (var tool in tools)
        {
            index.TryAdd(tool.Name, tool);
        }

        return index.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Finds a tool by name.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <returns>The tool, or null.</returns>
    internal static AgentToolDescriptor? Find(FrozenDictionary<string, AgentToolDescriptor> index, string name) =>
        index.GetValueOrDefault(name);
}
