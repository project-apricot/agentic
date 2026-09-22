namespace ApricotFramework.Agentic.Tools.Tests.Discovery;

/// <summary>An internal tool, which a host may reasonably still want found.</summary>
internal sealed class InternalTool : DiscoverableToolBase
{
    /// <inheritdoc />
    public override string Name => "discovery_items_internal";
}
