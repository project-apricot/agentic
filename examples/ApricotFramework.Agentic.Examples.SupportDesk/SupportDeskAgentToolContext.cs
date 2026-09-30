using ApricotFramework.Agentic.Tools;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// A call, plus the one thing this application knows about a caller that the library does not.
/// </summary>
/// <remarks>
/// <para>
/// The extension point <see cref="AgentToolContext"/> exists for. Surfaces are this application's
/// idea: which ones there are, whether a tool naming none belongs everywhere or nowhere, and
/// whether the distinction applies at all are questions about one product, so the library carries
/// no property for them. The tool side of the same question is a label - see
/// <see cref="SupportDeskLabels.Surfaces"/>.
/// </para>
/// <para>
/// Deriving rather than using <see cref="AgentToolContext.Items"/> because this application's own
/// filters and tools read it constantly, and a named property that the compiler checks beats a
/// dictionary lookup and a cast. Anything meant to travel between applications should use
/// <see cref="AgentToolContext.Items"/> instead.
/// </para>
/// </remarks>
public sealed class SupportDeskAgentToolContext : AgentToolContext
{
    /// <summary>
    /// Gets the surface the call arrived on, or null where the caller named none.
    /// </summary>
    /// <remarks>
    /// Read as <c>(context as SupportDeskAgentToolContext)?.Surface</c>, so a filter or a tool
    /// still works when a host - a test, a worker, another front end - passes a plain
    /// <see cref="AgentToolContext"/>. That is the point of deriving rather than replacing.
    /// </remarks>
    public string? Surface { get; init; }
}
