namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A single-result tool.</summary>
public sealed class SingleProbe : AgentTool<ProbeArguments, ProbeResult>
{
    /// <inheritdoc />
    public override string Name => "probe_items_get";

    /// <inheritdoc />
    public override string Title => "Get an item";

    /// <inheritdoc />
    public override string Description => "Reads one item by its identifier.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<ProbeResult> ExecuteAsync(ProbeArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ProbeResult(arguments.Id, "probe", null));
    }
}
