namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// The tools a host registered in code.
/// </summary>
/// <remarks>
/// What a container hands over, wrapped so it looks like every other source. Nothing here can
/// fail at run time, which is the point of separating it: a host can tell the difference between
/// the tools it wrote and the tools it merely reached.
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
    /// reading metadata will find none, which for the authorization filter means refusing.
    /// A factory rather than a second constructor, because an empty collection would otherwise be
    /// ambiguous between the two.
    /// </remarks>
    public static StaticAgentToolSource For(params AgentTool[] tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        return new StaticAgentToolSource(tools.Select(tool => new AgentToolDescriptor(tool)));
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(this.tools);
    }
}
