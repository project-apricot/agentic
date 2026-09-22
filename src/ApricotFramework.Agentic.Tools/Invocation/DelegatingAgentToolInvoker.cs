namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// An optional base for an <see cref="IAgentToolInvoker"/> that passes calls through to another.
/// </summary>
/// <remarks>
/// <para>
/// Recommended as the base for anything chained around an invoker. Every member delegates, so a
/// wrapper overrides only what it changes and keeps working when the interface grows.
/// </para>
/// <para>
/// Note that <see cref="InvokeAsync"/> and <see cref="InvokeCompleteAsync"/> are separate calls
/// rather than one expressed in terms of the other. A wrapper that overrode only the streaming
/// one would miss every caller that cannot stream, so both are worth overriding when the point is
/// to see every invocation.
/// </para>
/// </remarks>
/// <param name="inner">The invoker to pass calls to.</param>
public abstract class DelegatingAgentToolInvoker(IAgentToolInvoker inner) : IAgentToolInvoker
{
    /// <summary>
    /// Gets the invoker calls are passed to.
    /// </summary>
    protected IAgentToolInvoker Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AgentTool>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default)
    {
        return this.Inner.GetAvailableToolsAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        return this.Inner.InvokeAsync(name, argumentsJson, context, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        return this.Inner.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
    }
}
