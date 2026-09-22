using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool gated by one requirement.</summary>
[RequireProbe("granted")]
public sealed class GatedProbe : InheritingProbeBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_gated";
}
