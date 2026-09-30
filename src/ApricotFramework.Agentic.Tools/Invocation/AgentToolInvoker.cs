using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Decides what a caller can see and may use, then runs it.
/// </summary>
/// <remarks>
/// Two layers, in a fixed order that registration cannot change: every
/// <see cref="IAgentToolFilter"/> first, then every <see cref="IAgentToolAuthorizationFilter"/>.
/// </remarks>
public class AgentToolInvoker : IAgentToolInvoker
{
    /// <summary>
    /// Every tool offered.
    /// </summary>
    private readonly IAgentToolRegistry registry;

    /// <summary>
    /// What decides whether a caller can see a tool.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolFilter> filters;

    /// <summary>
    /// What decides whether a caller may use a tool they can see.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolAuthorizationFilter> authorizationFilters;

    /// <summary>
    /// Creates a new instance of the invoker.
    /// </summary>
    /// <param name="registry">Every tool offered.</param>
    /// <param name="filters">What decides whether a caller can see a tool.</param>
    /// <param name="authorizationFilters">What decides whether a caller may use a tool they can see.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is null.</exception>
    /// <remarks>
    /// Neither layer at all means every tool is offered to every caller. That is a host saying there
    /// is nothing to decide, which is true of a command line tool and false of almost everything
    /// else.
    /// </remarks>
    public AgentToolInvoker(IAgentToolRegistry registry, IEnumerable<IAgentToolFilter>? filters = null, IEnumerable<IAgentToolAuthorizationFilter>? authorizationFilters = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        this.registry = registry;
        this.filters = filters is null ? [] : [.. filters];
        this.authorizationFilters = authorizationFilters is null ? [] : [.. authorizationFilters];
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Put to the same two layers that would refuse the call, so a tool listed here is one of the call
    /// accepts.
    /// </para>
    /// <para>
    /// This is ergonomics rather than control: enforcement is <see cref="InvokeAsync"/> and
    /// nothing else. A filter whose answer moves can disagree with itself between the listing and
    /// the call, and showing a tool that then refuses is the better failure - a tool silently
    /// missing is one nobody can diagnose.
    /// </para>
    /// </remarks>
    public async ValueTask<IReadOnlyList<AgentToolDescriptor>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tools = await this.registry.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        var available = new List<AgentToolDescriptor>(tools.Count);

        foreach (var tool in tools)
        {
            if ((await this.FilterAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed
                && (await this.AuthorizeAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed)
            {
                available.Add(tool);
            }
        }

        return available;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Both layers run before the first item, so a caller that is refused receives nothing rather
    /// than a truncated result.
    /// </remarks>
    public async IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var function = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        var arguments = AgentToolInvocation.Create(argumentsJson, context);

        if (function is AgentTool streaming)
        {
            await foreach (var item in streaming.InvokeStreamingAsync(arguments, cancellationToken).ConfigureAwait(false))
            {
                yield return Write(item, function);
            }

            yield break;
        }

        // a function that is not one of ours returns one value, whatever its declaration says
        yield return Write(await function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false), function);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// For a caller that cannot stream. A sequence tool's items are collected into an array,
    /// which is what its output schema describes either way.
    /// </para>
    /// <para>
    /// A failure part-way through a sequence fails the whole call, and the items already
    /// collected are discarded. Handing a model a truncated result it has no way to recognize as
    /// truncated is worse than handing it an error.
    /// </para>
    /// </remarks>
    public async Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var function = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        var arguments = AgentToolInvocation.Create(argumentsJson, context);

        return Write(await function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false), function);
    }

    /// <summary>
    /// Writes one result the way its tool writes results.
    /// </summary>
    /// <param name="value">What the tool returned.</param>
    /// <param name="function">The tool.</param>
    /// <returns>The result as JSON.</returns>
    private static string Write(object? value, AIFunction function) => JsonSerializer.Serialize(value, function.JsonSerializerOptions);

    /// <summary>
    /// Finds the tool, puts it to both layers in order, and insists it can be run here.
    /// </summary>
    /// <param name="name">The tool to find.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the function behind the tool.</returns>
    private async ValueTask<AIFunction> ResolveAsync(string name, AgentToolContext context, CancellationToken cancellationToken)
    {
        var descriptor = await this.registry.RequireAsync(name, context, cancellationToken).ConfigureAwait(false);

        // scope first. a tool the caller cannot see is not there, so it is refused as not-found and
        // authorization is never asked about it
        var scope = await this.FilterAsync(descriptor, context, cancellationToken).ConfigureAwait(false);

        if (!scope.IsAllowed)
        {
            throw new AgentToolFilteredException(descriptor.Name, scope.Reason!);
        }

        var permission = await this.AuthorizeAsync(descriptor, context, cancellationToken).ConfigureAwait(false);

        if (!permission.IsAllowed)
        {
            throw new AgentToolAccessDeniedException(permission.Reason!);
        }

        return descriptor.AsFunction() ?? throw new AgentToolNotInvocableException($"The tool '{descriptor.Name}' is declared here but nothing here can run it.");
    }

    /// <summary>
    /// Asks every filter whether the caller can see the tool, stopping at the first refusal.
    /// </summary>
    /// <param name="tool">The tool in question.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    /// <remarks>
    /// The first refusal wins, and the rest are not consulted. A caller learns one reason rather
    /// than all of them, which is the right amount: the others may only apply because the first
    /// did.
    /// </remarks>
    private async ValueTask<AgentToolFilterDecision> FilterAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken)
    {
        foreach (var filter in this.filters)
        {
            var decision = await filter.EvaluateAsync(tool, context, cancellationToken).ConfigureAwait(false);

            if (!decision.IsAllowed)
            {
                return decision;
            }
        }

        return AgentToolFilterDecision.Allow();
    }

    /// <summary>
    /// Asks every authorization filter whether the caller may use the tool, stopping at the first refusal.
    /// </summary>
    /// <param name="tool">The tool in question.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    private async ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken)
    {
        foreach (var filter in this.authorizationFilters)
        {
            var decision = await filter.AuthorizeAsync(tool, context, cancellationToken).ConfigureAwait(false);

            if (!decision.IsAllowed)
            {
                return decision;
            }
        }

        return AgentToolAuthorizationDecision.Allow();
    }
}
