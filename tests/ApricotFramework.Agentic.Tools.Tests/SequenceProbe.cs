using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A sequence tool.</summary>
public sealed class SequenceProbe : ProbeStreamTool<ProbeNoArguments, ProbeResult>
{
    /// <inheritdoc />
    public override string Name => "probe_items_list";

    /// <inheritdoc />
    public override string Title => "List items";

    /// <inheritdoc />
    public override string Description => "Lists the items.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override async IAsyncEnumerable<ProbeResult> ExecuteAsync(ProbeNoArguments arguments, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 1; index <= 3; index++)
        {
            await Task.Yield();

            yield return new ProbeResult(index, $"item-{index}", null);
        }
    }
}
