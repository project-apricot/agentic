using System.Runtime.CompilerServices;
using System.Text.Json;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Answers what a caller could invoke and invokes it.
/// </summary>
/// <remarks>
/// <para>
/// The filters run here rather than in each tool, and that placement is the point. A check each
/// tool is trusted to perform is a check a tool can forget, and the one that forgets looks exactly
/// like the ones that do not. One gate every call passes through cannot be forgotten by adding a
/// tool.
/// </para>
/// <para>
/// The same filters decide both questions, so a caller is never offered a tool that then refuses
/// them, nor refused one it was offered. That property is the reason a listing exists, and it
/// holds structurally rather than by anybody remembering to keep two code paths in a step.
/// </para>
/// <para>
/// What it does <em>not</em> do is reshaping a listing. Capping it, sorting it, or hiding something
/// deprecated but still callable are all a <see cref="DelegatingAgentToolInvoker"/>'s business,
/// and so is anything about the call rather than about what exists - a budget, a rate limit, an
/// approval.
/// </para>
/// </remarks>
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
    /// else - so the ASP.NET Core package registers the authorization filter for you and refuses
    /// to start if a tool declares authorization that nothing is enforcing.
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
    /// What a surface advertises. Filtered by the same filters that would refuse the call, so a
    /// tool listed here is one they accept.
    /// </para>
    /// <para>
    /// This is ergonomics rather than control: enforcement is <see cref="InvokeAsync"/> and
    /// nothing else. A filter whose answer moves - keyed on the time of day, or on a budget being
    /// spent - can disagree with itself between the listing and the call. Where it does,
    /// showing a tool that then refuses is the better failure. A tool silently missing is one
    /// nobody can diagnose.
    /// </para>
    /// </remarks>
    public async ValueTask<IReadOnlyList<AgentTool>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tools = await this.registry.GetToolsAsync(cancellationToken).ConfigureAwait(false);

        var available = new List<AgentTool>(tools.Count);

        foreach (var tool in tools)
        {
            if ((await this.EvaluateAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed)
            {
                available.Add(tool.Tool);
            }
        }

        return available;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The filters run before the first item, so a caller that is refused receives nothing rather
    /// than a truncated result.
    /// </remarks>
    public async IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tool = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        await foreach (var item in tool.InvokeAsync(argumentsJson, context, cancellationToken).ConfigureAwait(false))
        {
            yield return JsonSerializer.Serialize(item, tool.SerializerOptions);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// For a caller that cannot stream. A sequence tool's items are collected into an array, which
    /// is what its output schema describes either way.
    /// </para>
    /// <para>
    /// A failure part-way through a sequence fails the whole call, and the items already collected
    /// are discarded. Handing a model a truncated result it has no way to recognize as truncated
    /// is worse than handing it an error.
    /// </para>
    /// </remarks>
    public async Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tool = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        if (tool.ResultKind == AgentToolResultKind.Whole)
        {
            var whole = await FirstOrDefaultAsync(tool, argumentsJson, context, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Serialize(whole, tool.SerializerOptions);
        }

        var items = new List<object?>();

        await foreach (var item in tool.InvokeAsync(argumentsJson, context, cancellationToken).ConfigureAwait(false))
        {
            items.Add(item);
        }

        return JsonSerializer.Serialize(items, tool.SerializerOptions);
    }

    /// <summary>
    /// Finds the tool and puts it to the filters.
    /// </summary>
    /// <param name="name">The tool to find.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tool.</returns>
    private async ValueTask<AgentTool> ResolveAsync(string name, AgentToolContext context, CancellationToken cancellationToken)
    {
        var tool = await this.registry.RequireAsync(name, cancellationToken).ConfigureAwait(false);

        var decision = await this.EvaluateAsync(tool, context, cancellationToken).ConfigureAwait(false);

        return decision.IsAllowed
            ? tool.Tool
            : throw new AgentToolAccessDeniedException(decision.Reason!);
    }

    /// <summary>
    /// Puts a tool to every filter, stopping at the first refusal.
    /// </summary>
    /// <param name="tool">The tool in question.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the decision.</returns>
    /// <remarks>
    /// The first refusal wins, and the rest are not consulted. A caller learns one reason rather than
    /// all of them, which is the right amount: the others may only apply because the first did.
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

    /// <summary>
    /// Reads the one result of a tool that returns its result whole.
    /// </summary>
    /// <param name="tool">The tool to run.</param>
    /// <param name="argumentsJson">The arguments as JSON.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result.</returns>
    private static async Task<object?> FirstOrDefaultAsync(AgentTool tool, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken)
    {
        await foreach (var item in tool.InvokeAsync(argumentsJson, context, cancellationToken).ConfigureAwait(false))
        {
            return item;
        }

        return null;
    }
}
