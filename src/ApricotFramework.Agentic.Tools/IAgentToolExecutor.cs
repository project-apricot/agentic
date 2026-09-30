using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// What a surface calls: name a tool, hand over arguments, get a result.
/// </summary>
/// <remarks>
/// <para>
/// There is no context parameter, and that is the point. Opening a scope and deciding who the
/// caller is are one question a host answers once, in an
/// <see cref="IAgentToolContextFactory"/> - not something every MCP handler, gRPC service and
/// HTTP endpoint re-derives, differently, with one of them eventually getting it wrong.
/// </para>
/// <para>
/// A scope is opened per call and disposed when the call ends, so a tool takes its dependencies
/// through its constructor exactly as an endpoint does, scoped ones included.
/// </para>
/// </remarks>
public interface IAgentToolExecutor
{
    /// <summary>
    /// Gets the tools the caller could invoke.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tools the caller could invoke, as functions a chat client can call.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the functions, ready for <c>ChatOptions.Tools</c>.</returns>
    /// <remarks>
    /// Each one runs through this executor rather than holding the scope it was listed in, so a
    /// conversation that lists once and calls twenty minutes later still runs each call in a
    /// scope of its own.
    /// </remarks>
    ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and reports its result as it arrives.
    /// </summary>
    /// <param name="name">The tool to run.</param>
    /// <param name="argumentsJson">The arguments as JSON or null where the tool takes none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result as JSON: one item for a whole result, one per item for a sequence.</returns>
    IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and reports its complete result.
    /// </summary>
    /// <param name="name">The tool to run.</param>
    /// <param name="argumentsJson">The arguments as JSON or null where the tool takes none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the complete result as JSON, matching the tool's output schema.</returns>
    Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default);
}
