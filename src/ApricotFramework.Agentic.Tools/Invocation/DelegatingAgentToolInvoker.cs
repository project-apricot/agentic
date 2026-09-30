namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// An invoker that passes everything through, so a wrapper says only what it changes.
/// </summary>
/// <remarks>
/// Where things that need to know the caller go: an audit record, a span carrying the user, a
/// per-person budget. Things that are about the surface rather than the caller wrap
/// <see cref="IAgentToolExecutor"/> instead.
/// </remarks>
/// <param name="inner">The invoker calls are passed to.</param>
public abstract class DelegatingAgentToolInvoker(IAgentToolInvoker inner) : IAgentToolInvoker
{
    /// <summary>
    /// Gets the invoker calls are passed to.
    /// </summary>
    protected IAgentToolInvoker Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableToolsAsync(context, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeAsync(name, argumentsJson, context, cancellationToken);

    /// <inheritdoc />
    public virtual Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
}
