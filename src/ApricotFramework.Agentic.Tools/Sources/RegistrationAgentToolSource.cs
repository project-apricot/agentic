using ApricotFramework.Agentic.Tools.Registration;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// The tools a host registered in code.
/// </summary>
/// <remarks>Described once on first use and cached; tool instances are still resolved per call from the caller's scope.</remarks>
public sealed class RegistrationAgentToolSource : IAgentToolSource
{
    /// <summary>
    /// Factories describing each registered tool.
    /// </summary>
    private readonly IReadOnlyList<Func<IServiceProvider, AgentToolDescriptor>> registrations;

    /// <summary>
    /// The descriptors and index, once built.
    /// </summary>
    /// <remarks>One object so readers never see a mismatched list and index.</remarks>
    private Described? described;

    /// <summary>
    /// Creates the source.
    /// </summary>
    /// <param name="options">The code registrations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public RegistrationAgentToolSource(IOptions<AgentToolRegistrationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.registrations = [.. options.Value.Registrations];
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.FromResult((Volatile.Read(ref this.described) ?? this.Describe(context.Services)).Tools);
    }

    /// <inheritdoc />
    public ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var held = Volatile.Read(ref this.described) ?? this.Describe(context.Services);

        return ValueTask.FromResult(AgentToolIndex.Find(held.Index, name));
    }

    /// <summary>
    /// Describes every registration, once.
    /// </summary>
    /// <param name="services">The scope to build declarations in.</param>
    /// <returns>The descriptors and index.</returns>
    /// <remarks>Lock-free: concurrent first callers may both describe; one result wins and the other is discarded.</remarks>
    private Described Describe(IServiceProvider services)
    {
        IReadOnlyList<AgentToolDescriptor> tools = [.. this.registrations.Select(registration => registration(services))];

        var fresh = new Described(tools, AgentToolIndex.By(tools));

        return Interlocked.CompareExchange(ref this.described, fresh, null) ?? fresh;
    }

    /// <summary>
    /// The described tools.
    /// </summary>
    /// <param name="Tools">The descriptors, in registration order.</param>
    /// <param name="Index">The descriptors by name.</param>
    private sealed record Described(IReadOnlyList<AgentToolDescriptor> Tools, FrozenDictionary<string, AgentToolDescriptor> Index);
}
