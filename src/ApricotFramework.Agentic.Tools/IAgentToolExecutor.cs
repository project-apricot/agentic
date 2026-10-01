using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Entry point for surfaces: lists tools and runs them by name with JSON arguments.
/// </summary>
/// <remarks>
/// The caller is resolved by the host's <see cref="IAgentToolContextFactory"/>, not passed in.
/// Each call gets its own DI scope, disposed when the call ends.
/// </remarks>
public interface IAgentToolExecutor
{
    /// <summary>
    /// Gets the tools the caller could invoke.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tools the caller could invoke, as functions for a chat client.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The functions, ready for <c>ChatOptions.Tools</c>.</returns>
    /// <remarks>
    /// Each function runs through this executor in a fresh scope; it does not hold the listing's scope.
    /// </remarks>
    ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds one tool the caller could invoke, as a function that runs through this executor.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The function, or null if not offered or not permitted.</returns>
    /// <remarks>
    /// Avoids composing and authorizing every tool (e.g. for MCP <c>tools/call</c>); must agree with
    /// <see cref="GetAvailableFunctionsAsync"/>.
    /// </remarks>
    ValueTask<AIFunction?> GetAvailableFunctionAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool, streaming its result.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="argumentsJson">The arguments as JSON, or null if none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>JSON: one item for a whole result, one per item for a sequence.</returns>
    IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a tool and returns its complete result.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="argumentsJson">The arguments as JSON, or null if none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete result as JSON, matching the tool's output schema.</returns>
    Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default);
}
