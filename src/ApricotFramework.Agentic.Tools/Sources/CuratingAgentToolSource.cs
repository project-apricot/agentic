namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// A source whose tools have been looked over before being offered.
/// </summary>
/// <remarks>
/// <para>
/// What a host wraps a source in when it does not control what that source says. Each tool is
/// passed to the curator, which returns it to offer it, a different descriptor to offer it
/// differently, or null to leave it out.
/// </para>
/// <para>
/// Both halves matter, and for different failures. Leaving a tool out keeps one malformed
/// declaration from stopping a host that has thirty good ones. Offering it differently is what
/// answers the declaration a host could have accepted if only it said something - a name that
/// collides, prose written for another model, or no authorization at all.
/// </para>
/// <para>
/// See <see cref="AgentToolCuration"/> for the ready-made curators, and nest these to apply more
/// than one.
/// </para>
/// </remarks>
/// <param name="inner">The source whose tools to look over.</param>
/// <param name="curate">Decides what to do with each tool. Returns null to leave it out.</param>
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
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await this.inner.GetToolsAsync(cancellationToken).ConfigureAwait(false);

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
