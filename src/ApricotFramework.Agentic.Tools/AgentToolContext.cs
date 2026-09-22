using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Who is asking, and in what setting.
/// </summary>
/// <remarks>
/// <para>
/// A type rather than a parameter list, for two reasons. The first is that what a call needs to
/// carry is not a settled question - a correlation id, a locale, a tenant, a budget - and every
/// answer to it would otherwise be a change to the signature of every tool. The second is that
/// hosts differ: one call arrives on an HTTP request and the next on a timer with no request
/// anywhere in sight, and both are ordinary.
/// </para>
/// <para>
/// Not sealed. A host with state worth naming derives from this and adds it, so its own tools read
/// a property instead of pulling an object out of a dictionary:
/// </para>
/// <code>
/// public sealed class HttpAgentToolContext: AgentToolContext
/// {
///     public required HttpContext Context { get; init; }
/// }
/// </code>
/// <para>
/// Which surface a call arrived on belongs here too, where a host distinguishes them. There is no
/// property for it, because which surfaces exist - and whether the idea applies at all - is one
/// application's business; the tool side of the same question is a
/// <see cref="AgentTool.Labels">label</see>.
/// </para>
/// <para>
/// A tool written against the base type keeps working either way, which is the point: deriving is
/// for the host's own tools, and anything meant to be portable should reach for
/// <see cref="Services"/> or <see cref="Items"/> instead.
/// </para>
/// </remarks>
public class AgentToolContext
{
    /// <summary>
    /// Gets the caller or null where the host tracks none.
    /// </summary>
    /// <remarks>
    /// Null is different from anonymous. A host with no notion of a caller at all - a command
    /// line tool, a scheduled job acting as itself - leaves this unset, and its authorizer decides
    /// what that means rather than this library assuming.
    /// </remarks>
    public ClaimsPrincipal? User { get; init; }

    /// <summary>
    /// Gets the services available for this invocation.
    /// </summary>
    /// <remarks>
    /// The portable way to reach the host state: a scoped unit of work, an HTTP context accessor, a
    /// clock. Preferred over <see cref="Items"/> where the thing wanted is a service, and over
    /// deriving where the tool is not the host's own.
    /// </remarks>
    public IServiceProvider? Services { get; init; }

    /// <summary>
    /// Gets where a tool reports how far along it is if the caller is listening.
    /// </summary>
    /// <remarks>
    /// Null when nobody asked, which is the common case - a tool reports progress only when there
    /// is somewhere for it to go. A host wanting it per call rather than per caller builds a
    /// context per call, which costs nothing; the point is that it no longer has to.
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
