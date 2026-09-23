namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// A context with a scope and no caller.
/// </summary>
/// <remarks>
/// What a host that has no notion of a person gets: a console tool, a scheduled job, a test. Null
/// is not anonymous - a host's filters decide what an absent caller means rather than this
/// deciding for them.
/// </remarks>
public sealed class DefaultAgentToolContextFactory : IAgentToolContextFactory
{
    /// <inheritdoc />
    public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopedServices);

        return ValueTask.FromResult(new AgentToolContext { Services = scopedServices });
    }
}
