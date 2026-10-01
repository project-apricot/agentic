namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Builds the <see cref="AgentToolContext"/> for a call, i.e. decides who is asking.
/// </summary>
/// <remarks>
/// One per host. A host may return a derived <see cref="AgentToolContext"/> that its tools cast to.
/// </remarks>
public interface IAgentToolContextFactory
{
    /// <summary>
    /// Builds the context for one call.
    /// </summary>
    /// <param name="scopedServices">The scope opened for this call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The context.</returns>
    ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default);
}
