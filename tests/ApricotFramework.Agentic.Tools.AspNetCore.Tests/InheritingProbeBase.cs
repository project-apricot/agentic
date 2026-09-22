using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool base carrying a requirement its derivatives inherit.</summary>
[RequireProbe("inherited")]
public abstract class InheritingProbeBase : AgentTool<AgentToolNoArguments, string>
{
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
