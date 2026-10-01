using System.Collections.Frozen;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// A source over a fixed list, the same for every caller.
/// </summary>
public sealed class StaticAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// The tools.
    /// </summary>
    private readonly IReadOnlyList<AgentToolDescriptor> tools;

    /// <summary>
    /// The tools by name.
    /// </summary>
    private readonly FrozenDictionary<string, AgentToolDescriptor> index;

    /// <summary>
    /// Creates the source.
    /// </summary>
    /// <param name="tools">The tools.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tools"/> is null.</exception>
    public StaticAgentToolSource(IEnumerable<AgentToolDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        this.tools = [.. tools];
        this.index = AgentToolIndex.By(this.tools);
    }

    /// <summary>
    /// Creates a source over tools with no metadata.
    /// </summary>
    /// <param name="tools">The tools.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tools"/> is null.</exception>
    /// <remarks>With no metadata, the authorization filter refuses these tools.</remarks>
    public static StaticAgentToolSource For(params AgentTool[] tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        return new StaticAgentToolSource(tools.Select(tool => new AgentToolDescriptor(tool)));
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(this.tools);

    /// <inheritdoc />
    public ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(AgentToolIndex.Find(this.index, name));
}
