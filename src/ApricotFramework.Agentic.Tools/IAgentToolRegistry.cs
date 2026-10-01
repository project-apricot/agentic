namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Composes the tools offered by all registered sources.
/// </summary>
/// <remarks>
/// Composed per call, since a source may answer differently per caller or over time; caching is
/// left to the sources.
/// </remarks>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Gets every tool offered to this caller, unfiltered.
    /// </summary>
    /// <param name="context">Who is asking, and the scope to run in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    /// <remarks>Filters are applied by the invoker, not here.</remarks>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the tool offered under a name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="context">Who is asking, and the scope to run in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool, or null if none.</returns>
    ValueTask<AgentToolDescriptor?> FindAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tool offered under a name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="context">Who is asking, and the scope to run in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="Exceptions.AgentToolNotFoundException">No tool has that name.</exception>
    ValueTask<AgentToolDescriptor> RequireAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default);
}
