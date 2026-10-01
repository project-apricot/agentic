namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// A source whose tools are curated before being offered.
/// </summary>
/// <remarks>For external tools, so one bad declaration costs that tool rather than all of them.</remarks>
/// <param name="inner">The curated source.</param>
/// <param name="curate">Returns the tool to offer, or null to drop it.</param>
public sealed class CuratingAgentToolSource(IAgentToolSource inner, Func<AgentToolDescriptor, AgentToolDescriptor?> curate) : IAgentToolSource
{
    /// <summary>
    /// The curated source.
    /// </summary>
    private readonly IAgentToolSource inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>
    /// Returns the tool to offer, or null to drop it.
    /// </summary>
    private readonly Func<AgentToolDescriptor, AgentToolDescriptor?> curate = curate ?? throw new ArgumentNullException(nameof(curate));

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        var tools = await this.inner.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        var curated = new List<AgentToolDescriptor>(tools.Count);

        foreach (var tool in tools)
        {
            if (this.curate(tool) is { } kept)
            {
                curated.Add(kept);
            }
        }

        return curated;
    }

    /// <inheritdoc />
    /// <remarks>Searches the curated listing, since curation may rename tools.</remarks>
    public async ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        var tools = await this.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        return tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    /// <remarks>Same as the inner source.</remarks>
    public bool IsExternal => this.inner.IsExternal;
}
