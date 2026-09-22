using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The log messages this example writes.
/// </summary>
/// <remarks>
/// Source-generated rather than written inline, because the analyzers insist and they are right
/// to: an interpolated message is formatted whether or not anything is listening.
/// </remarks>
public static partial class SupportDeskLog
{
    /// <summary>Records a tool an agent ran.</summary>
    /// <param name="logger">Where the record goes.</param>
    /// <param name="tool">The tool that ran.</param>
    /// <param name="subject">Who ran it.</param>
    /// <param name="surface">The surface they arrived on.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Agent tool {Tool} invoked by {Subject} on the {Surface} surface")]
    public static partial void ToolInvoked(ILogger logger, string tool, string subject, string surface);

    /// <summary>Records a foreign tool left out because its declaration would not pass.</summary>
    /// <param name="logger">Where the record goes.</param>
    /// <param name="reason">Why it was left out.</param>
    /// <param name="tool">The tool left out.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Left out the knowledge base tool {Tool}")]
    public static partial void ForeignToolRejected(ILogger logger, Exception reason, string tool);
}
