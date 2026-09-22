namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Every tool a host offers, addressed by name.
/// </summary>
/// <remarks>
/// <para>
/// Descriptor shaped, because this is the catalogue rather than the surface: what a filter reads
/// is the metadata, and metadata lives on the descriptor. A surface advertises tools - see
/// <see cref="IAgentToolInvoker.GetAvailableToolsAsync"/>.
/// </para>
/// <para>
/// An interface because the default implementation composes its sources once and holds the
/// answer, which is right for tools declared in code and wrong for tools that come and go. A host
/// reaching for the second case replaces this rather than arguing with the first.
/// </para>
/// </remarks>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Gets every tool offered.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    /// <remarks>
    /// Unfiltered. Which of these a given caller may reach is decided by the filters, which the
    /// invoker applies.
    /// </remarks>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the tool offered under a name.
    /// </summary>
    /// <param name="name">The name to find.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tool or null where none is offered under that name.</returns>
    ValueTask<AgentToolDescriptor?> FindAsync(string? name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tool offered under a name.
    /// </summary>
    /// <param name="name">The name to find.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tool.</returns>
    /// <exception cref="Exceptions.AgentToolNotFoundException">Thrown when no tool is offered under that name.</exception>
    ValueTask<AgentToolDescriptor> RequireAsync(string? name, CancellationToken cancellationToken = default);
}
