using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Who is asking, and whatever else the host carries through a call.
/// </summary>
/// <remarks>
/// <para>
/// The caller, not the call. Which tool and what arguments are parameters, because a caller
/// usually makes several calls and a listing makes none.
/// </para>
/// <para>
/// Built by an <c>IAgentToolContextFactory</c> rather than by whoever reaches a surface, so that
/// one host answers the question once. Not sealed: a host with a state worth naming derives from
/// it, and a tool written against the base type still works.
/// </para>
/// </remarks>
public class AgentToolContext : IAgentToolSourceContext
{
    /// <inheritdoc />
    public ClaimsPrincipal? User { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Never null, because the executor always runs a call inside a scope. That is what lets a
    /// tool take its dependencies through its constructor the way an endpoint does.
    /// </remarks>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets where a tool reports how far along it is if the caller is listening.
    /// </summary>
    /// <remarks>
    /// Null when nobody asked, which is the common case - a tool reports progress only when there
    /// is somewhere for it to go.
    /// </remarks>
    public IProgress<AgentToolProgress>? Progress { get; init; }

    /// <summary>
    /// Gets the host's own state for this invocation.
    /// </summary>
    /// <remarks>
    /// The untyped escape hatch, for a host that wants to pass something through without deriving
    /// a context type for it.
    /// </remarks>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
