namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// A source whose tools are looked over before they are offered.
/// </summary>
/// <remarks>
/// For tools from somewhere a host does not control. The registry refuses a declaration it
/// cannot accept, which is right for tools you wrote - you should not ship one you got wrong -
/// and wrong for tools you merely reached, where one bad declaration would cost you all of them.
/// </remarks>
/// <param name="inner">The source whose tools are looked over.</param>
/// <param name="curate">Decides what to do with each tool.</param>
public sealed class CuratingAgentToolSource(IAgentToolSource inner, Func<AgentToolDescriptor, AgentToolDescriptor?> curate) : IAgentToolSource
{
    /// <summary>
    /// The source whose tools are looked over.
    /// </summary>
    private readonly IAgentToolSource inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>
    /// Decides what to do with each tool.
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
}
