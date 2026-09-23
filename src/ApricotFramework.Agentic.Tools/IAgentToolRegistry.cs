namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Every tool a caller could be offered, composed from the sources a host registered.
/// </summary>
/// <remarks>
/// Composed per call rather than once, because a source can answer differently for a different
/// caller or at a different moment. What that costs is bounded by the sources: each caches what
/// it can, and a declaration is validated once per instance rather than once per listing.
/// </remarks>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Gets every tool offered to this caller.
    /// </summary>
    /// <param name="context">Who is asking, and the scope this composition runs in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    /// <remarks>
    /// Unfiltered. Which of these the caller may actually reach is decided by the filters, which
    /// the invoker applies.
    /// </remarks>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(
        IAgentToolSourceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the tool offered under a name.
    /// </summary>
    /// <param name="name">The name to find.</param>
    /// <param name="context">Who is asking, and the scope this composition runs in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tool or null where none is offered under that name.</returns>
    ValueTask<AgentToolDescriptor?> FindAsync(
        string? name,
        IAgentToolSourceContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tool offered under a name.
    /// </summary>
    /// <param name="name">The name to find.</param>
    /// <param name="context">Who is asking, and the scope this composition runs in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tool.</returns>
    /// <exception cref="Exceptions.AgentToolNotFoundException">Thrown when no tool is offered under that name.</exception>
    ValueTask<AgentToolDescriptor> RequireAsync(
        string? name,
        IAgentToolSourceContext context,
        CancellationToken cancellationToken = default);
}
