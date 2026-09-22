using ApricotFramework.Agentic.Tools.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool that returns its result as a sequence of items.
/// </summary>
/// <typeparam name="TArguments">What the tool is called with. Use <see cref="AgentToolNoArguments"/> where it takes nothing.</typeparam>
/// <typeparam name="TItem">One item of the result.</typeparam>
/// <remarks>
/// <para>
/// Worth choosing when a caller can act on the first items before the last ones exist. A caller
/// that cannot stream collects the items and sees the same result as it would have from
/// <see cref="AgentTool{TArguments, TResult}"/>, so the choice costs such a caller nothing.
/// </para>
/// <para>
/// <see cref="AgentTool.OutputSchema"/> describes the assembled array rather than
/// <typeparamref name="TItem"/>, so the schema means the same thing for every tool.
/// </para>
/// </remarks>
public abstract class AgentStreamTool<TArguments, TItem> : AgentTool
{
    /// <inheritdoc />
    public sealed override AgentToolResultKind ResultKind => AgentToolResultKind.Sequence;

    /// <inheritdoc />
    public sealed override JsonElement InputSchema => AgentToolJson.Schema<TArguments>(this.SerializerOptions);

    /// <inheritdoc />
    public sealed override JsonElement? OutputSchema => AgentToolJson.Schema<IReadOnlyList<TItem>>(this.SerializerOptions);

    /// <inheritdoc />
    public sealed override async IAsyncEnumerable<object?> InvokeAsync(string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var arguments = AgentToolArguments.Read<TArguments>(argumentsJson, this.SerializerOptions);

        await foreach (var item in this.ExecuteAsync(arguments, context, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Runs the tool against arguments already read.
    /// </summary>
    /// <param name="arguments">The arguments the caller supplied.</param>
    /// <param name="context">The invocation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The items forming the result.</returns>
    protected abstract IAsyncEnumerable<TItem> ExecuteAsync(TArguments arguments, AgentToolContext context, CancellationToken cancellationToken);
}
