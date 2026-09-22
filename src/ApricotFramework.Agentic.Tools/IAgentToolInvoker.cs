using ApricotFramework.Agentic.Tools.Invocation;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Answers what a caller could invoke and invokes it.
/// </summary>
/// <remarks>
/// <para>
/// An interface because a tool call is the natural place for the things a host wants around every
/// tool call: a log line, a span, a rate limit, a record of what an agent did on somebody's
/// behalf. Those are wrappers rather than replacements, and wrapping is what an interface allows
/// and a concrete class does not.
/// </para>
/// <para>
/// <see cref="DelegatingAgentToolInvoker"/> is the base to start from - it passes everything
/// through, so a wrapper says only what it changes.
/// </para>
/// <para>
/// The tool and its arguments are parameters rather than part of the context because they
/// describe one call where the context describes the caller. A loop asking five questions on
/// somebody's behalf builds the context at once.
/// </para>
/// <para>
/// Both questions are here on purpose. A host that wrapped the call but not the listing would
/// have a rate limit that counts invocations and a log that misses what was offered, and the two
/// would answer to different rules.
/// </para>
/// </remarks>
public interface IAgentToolInvoker
{
    /// <summary>
    /// Gets the tools this caller could invoke.
    /// </summary>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools the caller is permitted, on the surface they arrived on.</returns>
    ValueTask<IReadOnlyList<AgentTool>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default);

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
