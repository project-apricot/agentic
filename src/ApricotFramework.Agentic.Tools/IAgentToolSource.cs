namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A provider of tools.
/// </summary>
/// <remarks>
/// Asked on every listing and call, not once at startup, since tools may change at runtime or
/// differ per caller. Caching is the source's responsibility; the registry holds nothing between calls.
/// </remarks>
public interface IAgentToolSource
{
    /// <summary>
    /// Gets the tools this source offers to this caller.
    /// </summary>
    /// <param name="context">Who is asking, and the scope to run in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the tool this source offers this caller under a name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="context">Who is asking, and the scope to run in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool, or null if this source has none by that name.</returns>
    /// <remarks>
    /// Used on invocation; the registry stops at the first source that answers. Deliberately has no
    /// default so implementations answer cheaply rather than scanning the listing. Must agree with
    /// <see cref="GetToolsAsync"/> for the same caller.
    /// </remarks>
    ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether this source's tools come from outside this host's control.
    /// </summary>
    /// <remarks>
    /// When true, the registry logs and skips an invalid tool instead of failing. Defaults to false (strict).
    /// </remarks>
    bool IsExternal => false;
}
