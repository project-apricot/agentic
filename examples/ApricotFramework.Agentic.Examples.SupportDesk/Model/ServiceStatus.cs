using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Model;

/// <summary>What a service status check reports.</summary>
public sealed record ServiceStatus
{
    /// <summary>Whether everything is up.</summary>
    [Description("True when every dependency is reachable.")]
    public required bool Healthy { get; init; }

    /// <summary>What is down.</summary>
    [Description("The names of any dependencies that are not reachable.")]
    public required IReadOnlyList<string> Degraded { get; init; }

    /// <summary>When it was checked.</summary>
    [Description("When this was checked, in UTC. The answer is only true as of this moment.")]
    public required DateTime Checked { get; init; }
}
