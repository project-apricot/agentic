using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Filters;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Filters;

/// <summary>
/// Keeps a tool off a surface it was not declared for.
/// </summary>
/// <remarks>
/// <para>
/// Fifteen lines, and the reason the library does not model surfaces itself. Which surfaces exist,
/// whether a tool with no surface belongs everywhere or nowhere, and whether the whole idea
/// applies at all are questions about one application.
/// </para>
/// <para>
/// This one reads fail closed: a tool naming no surface is offered on none. A different
/// application would reasonably read it the other way, and would write the other fifteen lines.
/// </para>
/// <para>
/// Being a filter, it applies to the listing and the call alike - so a tool kept off the MCP
/// surface is neither advertised there nor reachable by name.
/// </para>
/// </remarks>
public sealed class SurfaceAgentToolFilter : IAgentToolFilter
{
    /// <inheritdoc />
    public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(context);

        var surface = (context as SupportDeskAgentToolContext)?.Surface;

        if (string.IsNullOrWhiteSpace(surface))
        {
            // no surface named, so there is nothing to check against
            return ValueTask.FromResult(AgentToolFilterDecision.Allow());
        }

        var surfaces = tool.Declaration.TryGetLabel<string>(SupportDeskLabels.Surfaces, out var declared) ? declared! : string.Empty;

        var allowed = surfaces
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(surface, StringComparer.Ordinal);

        return ValueTask.FromResult(allowed
            ? AgentToolFilterDecision.Allow()
            : AgentToolFilterDecision.Deny($"The tool '{tool.Name}' is not offered on the '{surface}' surface."));
    }
}
