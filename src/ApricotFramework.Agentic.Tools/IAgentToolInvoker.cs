namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Lists and runs tools for a caller whose context is already built.
/// </summary>
/// <remarks>
/// <see cref="IAgentToolExecutor"/> opens the scope and builds the context, then delegates here.
/// Decorate this for concerns that need the caller (e.g. auditing); decorate the executor otherwise.
/// </remarks>
public interface IAgentToolInvoker
{
    /// <summary>
    /// Gets the tools this caller could invoke.
    /// </summary>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The permitted tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds one tool this caller could invoke.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool, or null if not offered or not permitted.</returns>
    /// <remarks>
    /// Should evaluate only this tool, not compose the whole listing, and must agree with
    /// <see cref="GetAvailableToolsAsync"/>.
    /// </remarks>
    ValueTask<AgentToolDescriptor?> FindAvailableToolAsync(string name, AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool, streaming its result.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="argumentsJson">The arguments as JSON, or null if none.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>JSON: one item for a whole result, one per item for a sequence.</returns>
    IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and returns its complete result.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="argumentsJson">The arguments as JSON, or null if none.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete result as JSON, matching the tool's output schema.</returns>
    Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default);
}
