using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by a named policy and a requirement attribute together.</summary>
[Authorize(Policy = "tools.use")]
[RequireProbe("granted")]
public sealed class MixedProbe : ProbeToolBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_mixed";

}
