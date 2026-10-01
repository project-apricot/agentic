using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Opens a scope, resolves the caller, and runs the call inside both.
/// </summary>
public class AgentToolExecutor : IAgentToolExecutor
{
    /// <summary>
    /// Source of each call's scope.
    /// </summary>
    private readonly IServiceScopeFactory scopes;

    /// <summary>
    /// Creates the executor.
    /// </summary>
    /// <param name="scopes">Source of each call's scope.</param>
    /// <exception cref="ArgumentNullException"><paramref name="scopes"/> is null.</exception>
    public AgentToolExecutor(IServiceScopeFactory scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        this.scopes = scopes;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await CreateContextAsync(scope, cancellationToken).ConfigureAwait(false);

        return await Invoker(scope).GetAvailableToolsAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await this.GetAvailableToolsAsync(cancellationToken).ConfigureAwait(false);

        return [.. tools.Select(AIFunction (tool) => new ExecutorAgentTool(tool, this))];
    }

    /// <inheritdoc />
    /// <remarks>Resolves only the one tool: its source, filters and authorization.</remarks>
    public async ValueTask<AIFunction?> GetAvailableFunctionAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await CreateContextAsync(scope, cancellationToken).ConfigureAwait(false);

        var tool = await Invoker(scope).FindAvailableToolAsync(name, context, cancellationToken).ConfigureAwait(false);

        return tool is null ? null : new ExecutorAgentTool(tool, this);
    }

    /// <inheritdoc />
    /// <remarks>The scope lives until the last item is yielded or the caller stops reading, so a tool can hold a unit of work open across the sequence.</remarks>
    public async IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await CreateContextAsync(scope, cancellationToken).ConfigureAwait(false);

        await foreach (var chunk in Invoker(scope).InvokeAsync(name, argumentsJson, context, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public async Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await CreateContextAsync(scope, cancellationToken).ConfigureAwait(false);

        return await Invoker(scope).InvokeCompleteAsync(name, argumentsJson, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves the caller for one call.
    /// </summary>
    /// <param name="scope">The call's scope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The context.</returns>
    internal static ValueTask<AgentToolContext> CreateContextAsync(AsyncServiceScope scope, CancellationToken cancellationToken) =>
        scope.ServiceProvider.GetRequiredService<IAgentToolContextFactory>().CreateAsync(scope.ServiceProvider, cancellationToken);

    /// <summary>
    /// Gets the invoker for one call.
    /// </summary>
    /// <param name="scope">The call's scope.</param>
    /// <returns>The invoker.</returns>
    /// <remarks>Resolved from the scope so scoped filters (e.g. per-request tenant) are honored.</remarks>
    private static IAgentToolInvoker Invoker(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IAgentToolInvoker>();
}
