using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// A tool written by hand against <see cref="AgentTool"/> whose result arrives as a sequence.
/// </summary>
/// <typeparam name="TArguments">What the caller supplies.</typeparam>
/// <typeparam name="TItem">One item of the result.</typeparam>
/// <remarks>
/// Test infrastructure. Deriving <see cref="AgentTool"/> is the only way to produce items rather
/// than one value, which is what <see cref="AgentTool.InvokeStreamingAsync"/> exists for.
/// </remarks>
public abstract class ProbeStreamTool<TArguments, TItem> : AgentTool
{
    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => AgentToolResultKind.Sequence;

    /// <inheritdoc />
    public override JsonElement JsonSchema => AgentToolJson.Schema<TArguments>(this.JsonSerializerOptions);

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => AgentToolJson.Schema<IReadOnlyList<TItem>>(this.JsonSerializerOptions);

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeStreamingAsync(
        AIFunctionArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var read = ProbeArgumentReader.Read<TArguments>(arguments, this.JsonSerializerOptions);

        await foreach (var item in this.ExecuteAsync(read, arguments.RequireAgentToolContext(), cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var items = new List<TItem>();

        var read = ProbeArgumentReader.Read<TArguments>(arguments, this.JsonSerializerOptions);

        await foreach (var item in this.ExecuteAsync(read, arguments.RequireAgentToolContext(), cancellationToken).ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items;
    }

    /// <summary>Runs the tool.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The items.</returns>
    protected abstract IAsyncEnumerable<TItem> ExecuteAsync(TArguments arguments, AgentToolContext context, CancellationToken cancellationToken);
}
