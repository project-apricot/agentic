namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool declaring no requirement at all.</summary>
public sealed class UngatedProbe : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => "probe_items_ungated";

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Does a thing.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
