using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a role.</summary>
[Authorize(Roles = "curator")]
public sealed class RoleProbe : ProbeToolBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_role";

}
