using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// Composes every source for the caller.
/// </summary>
public class AgentToolRegistry : IAgentToolRegistry
{
    /// <summary>
    /// Tool sources, the host's own first.
    /// </summary>
    /// <remarks>Ordered once so a name clash always resolves to the host's own tool, whatever the registration order.</remarks>
    private readonly IReadOnlyList<IAgentToolSource> sources;

    /// <summary>
    /// Logs dropped external tools.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// Host validators applied to each declaration.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolValidator> validators;

    /// <summary>
    /// Descriptors already validated.
    /// </summary>
    /// <remarks>Keyed by instance, so a source returning the same descriptors is validated once per process.</remarks>
    private readonly ConditionalWeakTable<AgentToolDescriptor, object> checkedAlready = [];

    /// <summary>
    /// Creates the registry.
    /// </summary>
    /// <param name="sources">Tool sources.</param>
    /// <param name="validators">Host validators, applied after the built-in checks.</param>
    /// <param name="logger">Logs dropped external tools; null for none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sources"/> is null.</exception>
    public AgentToolRegistry(IEnumerable<IAgentToolSource> sources, IEnumerable<IAgentToolValidator>? validators = null, ILogger<AgentToolRegistry>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var all = sources.ToList();

        this.sources = [.. all.Where(source => !source.IsExternal), .. all.Where(source => source.IsExternal)];
        this.validators = validators is null ? [] : [.. validators];
        this.logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    /// <remarks>A rejected or clashing tool from the host's own sources fails the listing. From an external source, rejected tools and source failures are logged and dropped.</remarks>
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var declared = new Dictionary<string, AgentToolDescriptor>(StringComparer.Ordinal);

        foreach (var source in this.sources)
        {
            var tools = await this.ListAsync(source, context, cancellationToken).ConfigureAwait(false);

            foreach (var tool in tools)
            {
                if (!this.Accept(tool, source))
                {
                    continue;
                }

                if (declared.TryAdd(tool.Name, tool))
                {
                    continue;
                }

                if (!source.IsExternal)
                {
                    throw new AgentToolDeclarationException(
                        $"Two tools are offered as '{tool.Name}'. A tool name is a contract and has to address one operation.");
                }

                AgentToolRegistryLog.NameTaken(this.logger, tool.Name, Describe(source));
            }
        }

        return [.. declared.Values];
    }

    /// <inheritdoc />
    /// <remarks>Asks sources in order, the host's own first, stopping at the first that offers the name, so unrelated sources need not be up.</remarks>
    public async ValueTask<AgentToolDescriptor?> FindAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        foreach (var source in this.sources)
        {
            var tool = await this.FindAsync(source, name, context, cancellationToken).ConfigureAwait(false);

            if (tool is not null && this.Accept(tool, source))
            {
                return tool;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<AgentToolDescriptor> RequireAsync(string? name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        return await this.FindAsync(name, context, cancellationToken).ConfigureAwait(false)
               ?? throw new AgentToolNotFoundException($"No tool is offered as '{name}'.");
    }

    /// <summary>
    /// Reads one source's listing.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Its tools, or none if an external source failed.</returns>
    private async ValueTask<IReadOnlyList<AgentToolDescriptor>> ListAsync(IAgentToolSource source, IAgentToolSourceContext context, CancellationToken cancellationToken)
    {
        if (!source.IsExternal)
        {
            return await source.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await source.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (Tolerable(exception, cancellationToken))
        {
            AgentToolRegistryLog.SourceFailed(this.logger, exception, Describe(source));

            return [];
        }
    }

    /// <summary>
    /// Asks one source for a name.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="name">The name.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool, or null if not offered or an external source failed.</returns>
    private async ValueTask<AgentToolDescriptor?> FindAsync(IAgentToolSource source, string name, IAgentToolSourceContext context, CancellationToken cancellationToken)
    {
        if (!source.IsExternal)
        {
            return await source.FindAsync(name, context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await source.FindAsync(name, context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (Tolerable(exception, cancellationToken))
        {
            AgentToolRegistryLog.SourceFailed(this.logger, exception, Describe(source));

            return null;
        }
    }

    /// <summary>
    /// Decides whether a tool can be offered.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="source">Its source.</param>
    /// <returns>False if an external source's tool was rejected.</returns>
    /// <exception cref="AgentToolDeclarationException">A tool from the host's own sources was rejected.</exception>
    private bool Accept(AgentToolDescriptor tool, IAgentToolSource source)
    {
        if (!source.IsExternal)
        {
            this.Validate(tool);

            return true;
        }

        try
        {
            this.Validate(tool);

            return true;
        }
        catch (AgentToolDeclarationException exception)
        {
            AgentToolRegistryLog.Rejected(this.logger, exception, tool.Name, Describe(source));

            return false;
        }
    }

    /// <summary>
    /// Decides whether an external source's failure is logged and dropped.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>True if the source should be dropped.</returns>
    /// <remarks>Caller cancellation and <see cref="AgentToolConfigurationException"/> always propagate.</remarks>
    private static bool Tolerable(Exception exception, CancellationToken cancellationToken) =>
        exception is not AgentToolConfigurationException
        && (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested);

    /// <summary>
    /// Names a source for logging.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>Its name.</returns>
    private static string Describe(IAgentToolSource source) => source.ToString() ?? source.GetType().Name;

    /// <summary>
    /// Validates one declaration, once.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <remarks>Built-in checks cover only what addressing requires; anything else is an opt-in <see cref="IAgentToolValidator"/>.</remarks>
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
