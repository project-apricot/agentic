using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// The caller and host state carried through a tool call.
/// </summary>
/// <remarks>
/// Built by an <c>IAgentToolContextFactory</c>. Not sealed: hosts may derive their own context type.
/// </remarks>
public class AgentToolContext : IAgentToolSourceContext
{
    /// <inheritdoc />
    public ClaimsPrincipal? User { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Never null; calls always run inside a scope.
    /// </remarks>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the progress sink, or null when the caller is not listening.
    /// </summary>
    public IProgress<AgentToolProgress>? Progress { get; init; }

    /// <summary>
    /// Gets untyped host state for this invocation.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
