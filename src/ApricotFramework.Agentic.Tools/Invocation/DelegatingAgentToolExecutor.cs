using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Base executor that passes every call through, so a wrapper overrides only what it changes.
/// </summary>
/// <remarks>For surface concerns (rate limits, spans, listing caps). Caller-aware concerns should wrap <see cref="IAgentToolInvoker"/> instead.</remarks>
/// <param name="inner">The wrapped executor.</param>
public abstract class DelegatingAgentToolExecutor(IAgentToolExecutor inner) : IAgentToolExecutor
{
    /// <summary>
    /// Gets the wrapped executor.
    /// </summary>
    protected IAgentToolExecutor Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableToolsAsync(cancellationToken);

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableFunctionsAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>A wrapper that narrows the listing must narrow this too, or a lookup will find what the listing hid.</remarks>
    public virtual ValueTask<AIFunction?> GetAvailableFunctionAsync(string name, CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableFunctionAsync(name, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeAsync(name, argumentsJson, cancellationToken);

    /// <inheritdoc />
    public virtual Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeCompleteAsync(name, argumentsJson, cancellationToken);
}
