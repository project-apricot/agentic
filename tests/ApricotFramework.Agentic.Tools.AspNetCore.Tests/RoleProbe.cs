using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a role.</summary>
[Authorize(Roles = "curator")]
public sealed class RoleProbe : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => "probe_items_role";

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Gated by a role.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
