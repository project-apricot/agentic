namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Base invoker that passes every call through, so a wrapper overrides only what it changes.
/// </summary>
/// <remarks>For caller-aware concerns (audit, per-user spans, budgets). Surface concerns should wrap <see cref="IAgentToolExecutor"/> instead.</remarks>
/// <param name="inner">The wrapped invoker.</param>
public abstract class DelegatingAgentToolInvoker(IAgentToolInvoker inner) : IAgentToolInvoker
{
    /// <summary>
    /// Gets the wrapped invoker.
    /// </summary>
    protected IAgentToolInvoker Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableToolsAsync(context, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A wrapper that narrows the listing must narrow this too, or a lookup will find what the listing hid.</remarks>
    public virtual ValueTask<AgentToolDescriptor?> FindAvailableToolAsync(string name, AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.FindAvailableToolAsync(name, context, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeAsync(name, argumentsJson, context, cancellationToken);

    /// <inheritdoc />
    public virtual Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
}
