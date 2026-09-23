namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Listing and running tools for a caller whose context is already built.
/// </summary>
/// <remarks>
/// <para>
/// The inner half of running a tool. <see cref="IAgentToolExecutor"/> is what a surface calls; it
/// opens the scope and builds the context, then hands both to this. The two are separate because
/// they are wrapped for different reasons - an audit record needs the caller and belongs here, a
/// rate limit on a surface does not and belongs there.
/// </para>
/// <para>
/// One component answers what a caller could do and then does it, so a listing and a gate cannot
/// disagree.
/// </para>
/// </remarks>
public interface IAgentToolInvoker
{
    /// <summary>
    /// Gets the tools this caller could invoke.
    /// </summary>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools the caller is permitted.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and reports its result as it arrives.
    /// </summary>
    /// <param name="name">The tool to run.</param>
    /// <param name="argumentsJson">The arguments as JSON or null where the tool takes none.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result as JSON: one item for a whole result, one per item for a sequence.</returns>
    IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and reports its complete result.
    /// </summary>
    /// <param name="name">The tool to run.</param>
    /// <param name="argumentsJson">The arguments as JSON or null where the tool takes none.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the complete result as JSON, matching the tool's output schema.</returns>
    Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default);
}
