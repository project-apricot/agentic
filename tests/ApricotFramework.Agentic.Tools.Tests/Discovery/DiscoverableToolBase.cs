namespace ApricotFramework.Agentic.Tools.Tests.Discovery;

/// <summary>A base a family of discoverable tools shares. Abstract, so not itself a tool.</summary>
public abstract class DiscoverableToolBase : ProbeTool<ProbeNoArguments, string>
{
    /// <inheritdoc />
    public override string Title => "Discoverable";

    /// <inheritdoc />
    public override string Description => "Found by scanning.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(ProbeNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
