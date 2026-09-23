using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// The tools a host registered in code.
/// </summary>
/// <remarks>
/// Described once, the first time anything asks, and then held. What a registration describes -
/// a name, prose, two schemas - does not vary by caller, so there is nothing to recompute; what
/// does vary is the instance that runs, and that is resolved per call from the caller's scope.
/// </remarks>
public sealed class RegistrationAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// How to describe each registered tool.
    /// </summary>
    private readonly IReadOnlyList<Func<IServiceProvider, AgentToolDescriptor>> registrations;

    /// <summary>
    /// The descriptors, once they have been built.
    /// </summary>
    private IReadOnlyList<AgentToolDescriptor>? described;

    /// <summary>
    /// Creates a new instance of the source.
    /// </summary>
    /// <param name="options">The tools a host registered in code.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public RegistrationAgentToolSource(IOptions<AgentToolRegistrationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.registrations = [.. options.Value.Registrations];
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.FromResult(Volatile.Read(ref this.described) ?? this.Describe(context.Services));
    }

    /// <summary>
    /// Describes every registration, once.
    /// </summary>
    /// <param name="services">The scope to build the declarations in.</param>
    /// <returns>The descriptors.</returns>
    /// <remarks>
    /// Unlocked. Two callers arriving at once both describe, one of them wins the exchange, and
    /// the loser's instances are discarded - which costs a little repeated work exactly once in
    /// the life of the process and avoids holding a lock across whatever a constructor does.
    /// </remarks>
    private IReadOnlyList<AgentToolDescriptor> Describe(IServiceProvider services)
    {
        IReadOnlyList<AgentToolDescriptor> described = [.. this.registrations.Select(registration => registration(services))];

        return Interlocked.CompareExchange(ref this.described, described, null) ?? described;
    }
}
