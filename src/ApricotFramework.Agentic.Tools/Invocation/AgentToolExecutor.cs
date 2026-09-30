using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Opens a scope, decides who is asking, and runs the call inside both.
/// </summary>
/// <remarks>
/// The layer that was missing while every surface built its own context: there is one place
/// where a call acquires a scope and a caller, and every surface goes through it.
/// </remarks>
public class AgentToolExecutor : IAgentToolExecutor
{
    /// <summary>
    /// Where a call's scope comes from.
    /// </summary>
    private readonly IServiceScopeFactory scopes;

    /// <summary>
    /// How this host decides who is asking.
    /// </summary>
    private readonly IAgentToolContextFactory contexts;

    /// <summary>
    /// Creates a new instance of the executor.
    /// </summary>
    /// <param name="scopes">Where a call's scope comes from.</param>
    /// <param name="contexts">How this host decides who is asking.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public AgentToolExecutor(IServiceScopeFactory scopes, IAgentToolContextFactory contexts)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(contexts);

        this.scopes = scopes;
        this.contexts = contexts;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await this.contexts.CreateAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

        return await Invoker(scope).GetAvailableToolsAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await this.GetAvailableToolsAsync(cancellationToken).ConfigureAwait(false);

        return [.. tools.Select(AIFunction (tool) => new ExecutorAgentTool(tool, this))];
    }

    /// <inheritdoc />
    /// <remarks>
    /// The scope lives until the last item has been yielded or the caller stops reading,
    /// whichever comes first. A tool holding a unit of work open across a long sequence is the
    /// ordinary case, and disposing at the first item would break it.
    /// </remarks>
    public async IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await this.contexts.CreateAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

        await foreach (var chunk in Invoker(scope).InvokeAsync(name, argumentsJson, context, cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public async Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default)
    {
        await using var scope = this.scopes.CreateAsyncScope();

        var context = await this.contexts.CreateAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

        return await Invoker(scope).InvokeCompleteAsync(name, argumentsJson, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the invoker for one call.
    /// </summary>
    /// <param name="scope">The scope the call runs in.</param>
    /// <returns>The invoker.</returns>
    /// <remarks>
    /// Resolved from the scope rather than held, so that a host whose filters are scoped - one
    /// reading a per-request tenant, say - gets the scoped ones rather than a set captured when
    /// the executor was built.
    /// </remarks>
    private static IAgentToolInvoker Invoker(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IAgentToolInvoker>();
}
