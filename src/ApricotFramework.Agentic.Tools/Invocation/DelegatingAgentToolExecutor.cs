using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// An executor that passes everything through, so a wrapper says only what it changes.
/// </summary>
/// <remarks>
/// Where things about the surface go: a rate limit, a span, a cap on how many tools a listing
/// offers. Things that need the caller wrap <see cref="IAgentToolInvoker"/> instead, which is
/// inside the scope and has the context in hand.
/// </remarks>
/// <param name="inner">The executor calls are passed to.</param>
public abstract class DelegatingAgentToolExecutor(IAgentToolExecutor inner) : IAgentToolExecutor
{
    /// <summary>
    /// Gets the executor calls are passed to.
    /// </summary>
    protected IAgentToolExecutor Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableToolsAsync(cancellationToken);

    /// <inheritdoc />
    public virtual ValueTask<IReadOnlyList<AIFunction>> GetAvailableFunctionsAsync(CancellationToken cancellationToken = default) =>
        this.Inner.GetAvailableFunctionsAsync(cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeAsync(name, argumentsJson, cancellationToken);

    /// <inheritdoc />
    public virtual Task<string> InvokeCompleteAsync(string name, string? argumentsJson, CancellationToken cancellationToken = default) =>
        this.Inner.InvokeCompleteAsync(name, argumentsJson, cancellationToken);
}
