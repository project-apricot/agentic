using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools;
using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Auditing;

/// <summary>
/// Records every tool an agent ran on somebody's behalf.
/// </summary>
/// <remarks>
/// <para>
/// Why <c>IAgentToolInvoker</c> is an interface. Wrapping is how a host adds what belongs around
/// every call rather than inside every tool, and <see cref="DelegatingAgentToolInvoker"/> passes
/// the rest through, so this says only what it changes.
/// </para>
/// <para>
/// Both invocation methods are overridden, on purpose. They are separate calls rather than one
/// expressed in terms of the other, so a wrapper covering only the streaming one misses every
/// caller that cannot stream - and an audit log with a hole in it is worse than none.
/// </para>
/// </remarks>
/// <param name="inner">The invoker to pass calls to.</param>
/// <param name="logger">Where the record goes.</param>
public sealed class AuditingAgentToolInvoker(IAgentToolInvoker inner, ILogger<AuditingAgentToolInvoker> logger)
    : DelegatingAgentToolInvoker(inner)
{
    /// <inheritdoc />
    public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        Record(logger, name, context);

        return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        Record(logger, name, context);

        return base.InvokeAsync(name, argumentsJson, context, cancellationToken);
    }

    /// <summary>Writes the record.</summary>
    /// <param name="logger">Where the record goes.</param>
    /// <param name="name">The tool being run.</param>
    /// <param name="context">Who is running it.</param>
    private static void Record(ILogger logger, string name, AgentToolContext context)
    {
        SupportDeskLog.ToolInvoked(
            logger,
            name,
            context.User?.FindFirst("sub")?.Value ?? "(anonymous)",
            (context as SupportDeskAgentToolContext)?.Surface ?? "(unnamed)");
    }
}
