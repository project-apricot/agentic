using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// Composes the tools every source offers, checks them, and holds the answer.
/// </summary>
/// <remarks>
/// <para>
/// Declarations are checked as they are composed. A malformed tool is a host that refuses to
/// serve rather than a listing a consumer has already cached, and two tools claiming one name are
/// caught before either is advertised.
/// </para>
/// <para>
/// Composed once, on first use, and held. That is right for tools declared in code and wrong for
/// tools that come and go, which is why <see cref="IAgentToolRegistry"/> exists - a host with
/// sources that change replaces this rather than waiting for it to grow a refresh it would then
/// have to make thread-safe for everyone.
/// </para>
/// <para>
/// Composition being lazy means a malformed declaration surfaces on first use rather than at
/// startup. A host wanting the earlier failure asks for the tools while it is starting.
/// </para>
/// </remarks>
public class AgentToolRegistry : IAgentToolRegistry
{
    /// <summary>
    /// Where tools come from.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolSource> sources;

    /// <summary>
    /// The host's checks, applied to each declaration after the universal ones.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolValidator> validators;

    /// <summary>
    /// The composition, once it has been made.
    /// </summary>
    private Dictionary<string, AgentToolDescriptor>? composed;

    /// <summary>
    /// Creates a new instance of the registry.
    /// </summary>
    /// <param name="sources">Where tools come from.</param>
    /// <param name="validators">The host's checks, applied to each declaration after the universal ones.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null.</exception>
    public AgentToolRegistry(IEnumerable<IAgentToolSource> sources, IEnumerable<IAgentToolValidator>? validators = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        this.sources = sources.ToList();
        this.validators = validators?.ToList() ?? [];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = await this.ComposeAsync(cancellationToken).ConfigureAwait(false);

        return [.. tools.Values];
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolDescriptor?> FindAsync(string? name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var tools = await this.ComposeAsync(cancellationToken).ConfigureAwait(false);

        return tools.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolDescriptor> RequireAsync(string? name, CancellationToken cancellationToken = default)
    {
        return await this.FindAsync(name, cancellationToken).ConfigureAwait(false)
               ?? throw new AgentToolNotFoundException($"No tool is offered as '{name}'.");
    }

    /// <summary>
    /// Composes the sources, once.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools by name.</returns>
    /// <remarks>
    /// Unlocked. Two callers arriving at once both compose. One of them wins the exchange -
    /// which costs a little-repeated work exactly once in the life of the process and avoids
    /// holding a lock across whatever a source has to do to answer.
    /// </remarks>
    private async ValueTask<Dictionary<string, AgentToolDescriptor>> ComposeAsync(CancellationToken cancellationToken)
    {
        var existing = Volatile.Read(ref this.composed);

        if (existing is not null)
        {
            return existing;
        }

        var declared = new Dictionary<string, AgentToolDescriptor>(StringComparer.Ordinal);

        foreach (var source in this.sources)
        {
            foreach (var tool in await source.GetToolsAsync(cancellationToken).ConfigureAwait(false))
            {
                this.Validate(tool);

                if (!declared.TryAdd(tool.Name, tool))
                {
                    throw new AgentToolDeclarationException($"Two tools are offered as '{tool.Name}'. A tool name is a contract and has to address one operation.");
                }
            }
        }

        return Interlocked.CompareExchange(ref this.composed, declared, null) ?? declared;
    }

    /// <summary>
    /// Checks one declaration.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <remarks>
    /// Only what this class needs to address a tool. Everything a host could reasonably
    /// disagree about is an <see cref="IAgentToolValidator"/>, and none of those are registered
    /// unless a host asks for them.
    /// </remarks>
    private void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new AgentToolDeclarationException($"The tool '{tool.Tool.GetType().Name}' declares no name, so nothing can address it.");
        }

        foreach (var validator in this.validators)
        {
            validator.Validate(tool);
        }
    }
}
