using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool with no title and no description, to show nothing insists on either.</summary>
[RequireProbe("granted")]
public sealed class TerseProbe : ProbeToolBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_terse";

    /// <inheritdoc />
    public override string Title => "  ";

    /// <inheritdoc />
    public override string Description => "  ";
}
