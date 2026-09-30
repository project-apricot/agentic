namespace ApricotFramework.Agentic.Tools.Tests.Discovery;

/// <summary>A tool deliberately kept out of discovery.</summary>
[AgentToolIgnore]
public sealed class IgnoredTool : DiscoverableToolBase
{
    /// <inheritdoc />
    public override string Name => "discovery_items_ignored";
}
