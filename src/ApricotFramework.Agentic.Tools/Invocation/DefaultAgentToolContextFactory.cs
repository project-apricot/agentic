namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Creates a context with a scope and no caller.
/// </summary>
/// <remarks>For hosts with no notion of a person. A null caller is not anonymous; filters decide what it means.</remarks>
public sealed class DefaultAgentToolContextFactory : IAgentToolContextFactory
{
    /// <inheritdoc />
    public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopedServices);

        return ValueTask.FromResult(new AgentToolContext { Services = scopedServices });
    }
}
