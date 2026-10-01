using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// Registry log messages.
/// </summary>
/// <remarks>External sources only; problems with the host's own tools throw instead.</remarks>
internal static partial class AgentToolRegistryLog
{
    /// <summary>
    /// An external tool was rejected.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">Why it was rejected.</param>
    /// <param name="tool">The tool.</param>
    /// <param name="source">The source.</param>
    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "Left out the tool {Tool} offered by {Source}")]
    internal static partial void Rejected(ILogger logger, Exception exception, string tool, string source);

    /// <summary>
    /// An external tool's name was already taken.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tool">The name.</param>
    /// <param name="source">The source that offered it second.</param>
    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "Left out the tool {Tool} offered by {Source}: the name is already taken")]
    internal static partial void NameTaken(ILogger logger, string tool, string source);

    /// <summary>
    /// An external source failed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="source">The source.</param>
    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "Left out every tool from {Source}: it failed to answer")]
    internal static partial void SourceFailed(ILogger logger, Exception exception, string source);
}
