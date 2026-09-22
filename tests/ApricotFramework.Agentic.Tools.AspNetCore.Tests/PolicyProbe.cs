using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a named policy the host registers.</summary>
[Authorize(Policy = "tools.use")]
public sealed class PolicyProbe : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => "probe_items_policy";

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Gated by a named policy.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
