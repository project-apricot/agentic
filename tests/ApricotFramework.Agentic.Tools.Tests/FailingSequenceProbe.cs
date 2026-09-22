using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A tool that fails part way through its sequence.</summary>
public sealed class FailingSequenceProbe : AgentStreamTool<AgentToolNoArguments, ProbeResult>
{
    /// <inheritdoc />
    public override string Name => "probe_items_fail";

    /// <inheritdoc />
    public override string Title => "Fail";

    /// <inheritdoc />
    public override string Description => "Fails after the first item.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override async IAsyncEnumerable<ProbeResult> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        yield return new ProbeResult(1, "first", null);

        throw new InvalidOperationException("the source gave up");
    }
}
