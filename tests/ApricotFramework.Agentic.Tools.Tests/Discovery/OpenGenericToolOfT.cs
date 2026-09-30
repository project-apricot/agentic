namespace ApricotFramework.Agentic.Tools.Tests.Discovery;

/// <summary>An open generic, which cannot be instantiated as one tool.</summary>
/// <typeparam name="TResult">Whatever it returns.</typeparam>
public sealed class OpenGenericTool<TResult> : ProbeTool<ProbeNoArguments, TResult>
    where TResult : new()
{
    /// <inheritdoc />
    public override string Name => "discovery_items_generic";

    /// <inheritdoc />
    public override string Title => "Generic";

    /// <inheritdoc />
    public override string Description => "Never discovered.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<TResult> ExecuteAsync(ProbeNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult(new TResult());
}
