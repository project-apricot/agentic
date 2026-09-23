using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a named policy the host registers.</summary>
[Authorize(Policy = "tools.use")]
public sealed class PolicyProbe : ProbeToolBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_policy";

}
