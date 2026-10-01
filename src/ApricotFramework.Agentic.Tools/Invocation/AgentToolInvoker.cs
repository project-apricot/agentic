using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// Decides what a caller can see and may use, then runs it.
/// </summary>
/// <remarks>Every <see cref="IAgentToolFilter"/> runs before every <see cref="IAgentToolAuthorizationFilter"/>, regardless of registration order.</remarks>
public class AgentToolInvoker : IAgentToolInvoker
{
    /// <summary>
    /// Every tool offered.
    /// </summary>
    private readonly IAgentToolRegistry registry;

    /// <summary>
    /// Decide whether a caller can see a tool.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolFilter> filters;

    /// <summary>
    /// Decide whether a caller may use a visible tool.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolAuthorizationFilter> authorizationFilters;

    /// <summary>
    /// Translate host exceptions into <see cref="AgentToolException"/>.
    /// </summary>
    private readonly IReadOnlyList<IAgentToolExceptionTranslator> translators;

    /// <summary>
    /// Creates the invoker.
    /// </summary>
    /// <param name="registry">Every tool offered.</param>
    /// <param name="filters">Visibility filters.</param>
    /// <param name="authorizationFilters">Authorization filters.</param>
    /// <param name="translators">Exception translators; first answer wins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is null.</exception>
    /// <remarks>With no filters of either kind, every tool is offered to every caller.</remarks>
    public AgentToolInvoker(IAgentToolRegistry registry, IEnumerable<IAgentToolFilter>? filters = null, IEnumerable<IAgentToolAuthorizationFilter>? authorizationFilters = null, IEnumerable<IAgentToolExceptionTranslator>? translators = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        this.registry = registry;
        this.filters = filters is null ? [] : [.. filters];
        this.authorizationFilters = authorizationFilters is null ? [] : [.. authorizationFilters];
        this.translators = translators is null ? [] : [.. translators];
    }

    /// <inheritdoc />
    /// <remarks>Applies the same two layers as the call, but only as a convenience: enforcement happens in <see cref="InvokeAsync"/>. A filter whose answer changes may list a tool the call then refuses.</remarks>
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
    /// <remarks>Looks up the one name and applies both layers to that tool alone, agreeing with the listing.</remarks>
    public async ValueTask<AgentToolDescriptor?> FindAvailableToolAsync(string name, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tool = await this.registry.FindAsync(name, context, cancellationToken).ConfigureAwait(false);

        if (tool is null)
        {
            return null;
        }

        return (await this.FilterAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed
               && (await this.AuthorizeAsync(tool, context, cancellationToken).ConfigureAwait(false)).IsAllowed
            ? tool
            : null;
    }

    /// <inheritdoc />
    /// <remarks>Both layers run before the first item, so a refused caller receives nothing.</remarks>
    public async IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (descriptor, function) = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        var arguments = AgentToolInvocation.Create(argumentsJson, context);

        if (function is AgentTool streaming)
        {
            var items = streaming.InvokeStreamingAsync(arguments, cancellationToken).GetAsyncEnumerator(cancellationToken);

            await using (items.ConfigureAwait(false))
            {
                // moved by hand rather than with await foreach, because a failure has to be caught
                // and described while an item cannot be yielded from inside a catch
                while (await this.MoveNextAsync(items, descriptor).ConfigureAwait(false))
                {
                    yield return Write(items.Current, function);
                }
            }

            yield break;
        }

        // a function that is not one of ours returns one value, whatever its declaration says
        var result = await this.RunAsync(function, arguments, descriptor, cancellationToken).ConfigureAwait(false);

        yield return Write(result, function);
    }

    /// <inheritdoc />
    /// <remarks>A sequence tool's items are collected into an array. A failure part-way through fails the whole call rather than returning a truncated result.</remarks>
    public async Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var (descriptor, function) = await this.ResolveAsync(name, context, cancellationToken).ConfigureAwait(false);

        var arguments = AgentToolInvocation.Create(argumentsJson, context);

        return Write(await this.RunAsync(function, arguments, descriptor, cancellationToken).ConfigureAwait(false), function);
    }

    /// <summary>
    /// Runs a single-value tool, translating any failure.
    /// </summary>
    /// <param name="function">The tool's function.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="descriptor">The tool.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool's result.</returns>
    private async ValueTask<object?> RunAsync(AIFunction function, AIFunctionArguments arguments, AgentToolDescriptor descriptor, CancellationToken cancellationToken)
    {
        try
        {
            return await function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (this.Translate(exception, descriptor) is { } described)
        {
            throw described;
        }
    }

    /// <summary>
    /// Advances a tool's sequence by one, translating any failure.
    /// </summary>
    /// <param name="items">The sequence.</param>
    /// <param name="descriptor">The tool.</param>
    /// <returns>Whether there was another item.</returns>
    private async ValueTask<bool> MoveNextAsync(IAsyncEnumerator<object?> items, AgentToolDescriptor descriptor)
    {
        try
        {
            return await items.MoveNextAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (this.Translate(exception, descriptor) is { } described)
        {
            throw described;
        }
    }

    /// <summary>
    /// Translates a host exception via the first translator that recognizes it.
    /// </summary>
    /// <param name="exception">What the tool raised.</param>
    /// <param name="descriptor">The tool.</param>
    /// <returns>The translation, or null to rethrow unchanged.</returns>
    /// <remarks>Translators are asked in order; <see cref="AgentToolException"/> and cancellation are never offered.</remarks>
    private AgentToolException? Translate(Exception exception, AgentToolDescriptor descriptor)
    {
        if (exception is AgentToolException or OperationCanceledException)
        {
            return null;
        }

        foreach (var translator in this.translators)
        {
            if (translator.Translate(exception, descriptor) is { } described)
            {
                return described;
            }
        }

        return null;
    }

    /// <summary>
    /// Serializes one result with its tool's serializer options.
    /// </summary>
    /// <param name="value">The tool's result.</param>
    /// <param name="function">The tool.</param>
    /// <returns>The result as JSON.</returns>
    private static string Write(object? value, AIFunction function) => JsonSerializer.Serialize(value, function.JsonSerializerOptions);

    /// <summary>
    /// Finds the tool, applies both layers in order, and checks it can run here.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tool and its function.</returns>
    private async ValueTask<(AgentToolDescriptor Descriptor, AIFunction Function)> ResolveAsync(string name, AgentToolContext context, CancellationToken cancellationToken)
    {
        var descriptor = await this.registry.RequireAsync(name, context, cancellationToken).ConfigureAwait(false);
        var filterDecision = await this.FilterAsync(descriptor, context, cancellationToken).ConfigureAwait(false);

        if (!filterDecision.IsAllowed)
        {
            throw new AgentToolFilteredException(descriptor.Name, filterDecision.Reason!);
        }

        var authorizationDecision = await this.AuthorizeAsync(descriptor, context, cancellationToken).ConfigureAwait(false);

        if (!authorizationDecision.IsAllowed)
        {
            throw authorizationDecision.Outcome == AgentToolAuthorizationOutcome.Unauthenticated
                ? new AgentToolUnauthenticatedException(authorizationDecision.Reason!)
                : new AgentToolAccessDeniedException(authorizationDecision.Reason!);
        }

        var function = descriptor.AsFunction() ?? throw new AgentToolNotInvocableException($"The tool '{descriptor.Name}' is declared here but nothing here can run it.");

        return (descriptor, function);
    }

    /// <summary>
    /// Asks each filter whether the caller can see the tool; the first refusal wins.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision.</returns>
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
    /// Asks each authorization filter whether the caller may use the tool; the first refusal wins.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision.</returns>
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
