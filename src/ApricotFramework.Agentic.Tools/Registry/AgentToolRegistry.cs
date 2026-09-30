using ApricotFramework.Agentic.Tools.Exceptions;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// The registry: every source, composed for whoever is asking.
/// </summary>
public class AgentToolRegistry : IAgentToolRegistry
{
    /// <summary>
    /// Where tools come from.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolSource> sources;

    /// <summary>
    /// The host's checks, applied to each declaration as it arrives.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolValidator> validators;

    /// <summary>
    /// The descriptors already checked.
    /// </summary>
    /// <remarks>
    /// Keyed on the descriptor itself, so a source handing back the same instances - which a
    /// source with a fixed list does - is checked once in the life of the process rather than
    /// once per listing. A source that rebuilds its descriptors pays for each rebuild, which is
    /// right: those are new declarations and have not been looked at.
    /// </remarks>
    private readonly ConditionalWeakTable<AgentToolDescriptor, object> checkedAlready = [];

    /// <summary>
    /// Creates a new instance of the registry.
    /// </summary>
    /// <param name="sources">Where tools come from.</param>
    /// <param name="validators">The host's checks, applied to each declaration after the universal ones.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null.</exception>
    public AgentToolRegistry(IEnumerable<IAgentToolSource> sources, IEnumerable<IAgentToolValidator>? validators = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        this.sources = [.. sources];
        this.validators = validators is null ? [] : [.. validators];
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var declared = new Dictionary<string, AgentToolDescriptor>(StringComparer.Ordinal);

        foreach (var source in this.sources)
        {
            foreach (var tool in await source.GetToolsAsync(context, cancellationToken).ConfigureAwait(false))
            {
                this.Validate(tool);

                if (!declared.TryAdd(tool.Name, tool))
                {
                    throw new AgentToolDeclarationException(
                        $"Two tools are offered as '{tool.Name}'. A tool name is a contract and has to address one operation.");
                }
            }
        }

        return [.. declared.Values];
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolDescriptor?> FindAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var tools = await this.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        return tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolDescriptor> RequireAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        return await this.FindAsync(name, context, cancellationToken).ConfigureAwait(false)
               ?? throw new AgentToolNotFoundException($"No tool is offered as '{name}'.");
    }

    /// <summary>
    /// Checks one declaration, once.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <remarks>
    /// Only what this class needs to address a tool. Everything a host could reasonably disagree
    /// about is an <see cref="IAgentToolValidator"/>, and none of those are registered unless a
    /// host asks for them.
    /// </remarks>
    private void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (this.checkedAlready.TryGetValue(tool, out _))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new AgentToolDeclarationException($"A tool from '{tool.Tool.GetType().Name}' declares no name, so nothing can address it.");
        }

        // a tool declaring sequential output must be derived from AgentTool to implement async enumeration
        if (tool.Declaration.ResultKind == AgentToolResultKind.Sequence && tool.Tool is not AgentTool)
        {
            throw new AgentToolDeclarationException(
                $"The tool '{tool.Name}' declares a sequence result, but the function behind it returns one value and " +
                "cannot produce items. Declare it whole, or back it with an AgentTool that implements InvokeStreamingAsync.");
        }

        foreach (var validator in this.validators)
        {
            validator.Validate(tool);
        }

        // only once it passed. a declaration that threw has not been accepted, and the next
        // listing should say so again rather than quietly letting it through
        this.checkedAlready.AddOrUpdate(tool, this);
    }
}
