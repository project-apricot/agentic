using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a named policy and a requirement attribute together.</summary>
[Authorize(Policy = "tools.use")]
[RequireProbe("granted")]
public sealed class MixedProbe : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => "probe_items_mixed";

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Gated by both mechanisms.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
