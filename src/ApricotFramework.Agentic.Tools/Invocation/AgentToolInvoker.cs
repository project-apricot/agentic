using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Filters what a caller may reach, then runs it.
/// </summary>
public class AgentToolInvoker : IAgentToolInvoker
{
    /// <summary>
    /// Every tool offered.
    /// </summary>
    private readonly IAgentToolRegistry registry;

    /// <summary>
    /// What decides whether a caller may reach a tool.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolFilter> filters;

    /// <summary>
    /// Creates a new instance of the invoker.
    /// </summary>
    /// <param name="registry">Every tool offered.</param>
    /// <param name="filters">What decides whether a caller may reach a tool.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is null.</exception>
    /// <remarks>
    /// No filters at all mean every tool is offered to every caller. That is a host saying there
    /// is nothing to decide, which is true of a command line tool and false of almost everything
    /// else.
    /// </remarks>
    public AgentToolInvoker(IAgentToolRegistry registry, IEnumerable<IAgentToolFilter>? filters = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        this.registry = registry;
        this.filters = filters is null ? [] : [.. filters];
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Filtered by the same filters that would refuse the call, so a tool listed here is one they
    /// accept.
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
            if ((await this.EvaluateAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed)
            {
                available.Add(tool);
            }
        }

        return available;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The filters run before the first item, so a caller that is refused receives nothing rather
    /// than a truncated result.
    /// </remarks>
    public async IAsyncEnumerable<string> InvokeAsync(
        string name,
        string? argumentsJson,
        AgentToolContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
    /// collected are discarded. Handing a model a truncated result it has no way to recognise as
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
    private static string Write(object? value, AIFunction function) =>
        JsonSerializer.Serialize(value, function.JsonSerializerOptions);

    /// <summary>
    /// Finds the tool, puts it to the filters, and insists it can be run here.
    /// </summary>
    /// <param name="name">The tool to find.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the function behind the tool.</returns>
    private async ValueTask<AIFunction> ResolveAsync(
        string name,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var descriptor = await this.registry.RequireAsync(name, context, cancellationToken).ConfigureAwait(false);

        var decision = await this.EvaluateAsync(descriptor, context, cancellationToken).ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            throw new AgentToolAccessDeniedException(decision.Reason!);
        }

        return descriptor.AsFunction()
               ?? throw new AgentToolNotInvocableException($"The tool '{descriptor.Name}' is declared here but nothing here can run it.");
    }

    /// <summary>
    /// Puts a tool to every filter, stopping at the first refusal.
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
    private async ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken)
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
}
