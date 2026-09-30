namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// A source over a list that does not change.
/// </summary>
/// <remarks>
/// The same tools for every caller, which is what most hosts have. Ignores the context because
/// there is nothing about a caller that could change the answer.
/// </remarks>
public sealed class StaticAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// The tools.
    /// </summary>
    private readonly IReadOnlyList<AgentToolDescriptor> tools;

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="tools">The tools to offer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tools"/> is null.</exception>
    public StaticAgentToolSource(IEnumerable<AgentToolDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        this.tools = [.. tools];
    }

    /// <summary>
    /// Creates a source over tools nothing was said about.
    /// </summary>
    /// <param name="tools">The tools to offer.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tools"/> is null.</exception>
    /// <remarks>
    /// For a host with no metadata to attach - a test, or a loop running as itself. Anything
    /// reading metadata will find none, which for the authorization filter means refusing. A
    /// factory rather than a second constructor, because an empty collection would otherwise be
    /// ambiguous between the two.
    /// </remarks>
    public static StaticAgentToolSource For(params AgentTool[] tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        return new StaticAgentToolSource(tools.Select(tool => new AgentToolDescriptor(tool)));
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(this.tools);
}
