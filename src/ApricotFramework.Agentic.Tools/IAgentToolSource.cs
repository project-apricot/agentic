namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Somewhere tools come from.
/// </summary>
/// <remarks>
/// <para>
/// Asked on every listing and every call rather than once at startup, because not every source
/// can answer once. A source over upstream MCP servers has connections to make, tools that
/// change while the process runs, and - where a host connects servers per person - a different
/// answer for each caller.
/// </para>
/// <para>
/// Caching is therefore the source's own business, and the registry holds nothing between calls.
/// A source with a fixed list returns a field; one reaching a server caches per connection and
/// refreshes when the server says its tools changed.
/// </para>
/// <para>
/// A source that can fail decides for itself what failure means. Tools you wrote should stop the
/// host from starting. Tools from somewhere you do not control should be logged and left out -
/// see <c>CuratingAgentToolSource</c>.
/// </para>
/// </remarks>
public interface IAgentToolSource
{
    /// <summary>
    /// Gets the tools this source offers to this caller.
    /// </summary>
    /// <param name="context">Who is asking, and the scope this composition runs in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default);
}
