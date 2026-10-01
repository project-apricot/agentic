using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// Log messages for the gRPC tool source.
/// </summary>
internal static partial class GrpcAgentToolLog
{
    /// <summary>
    /// Logs a remote tool dropped because its schema could not be read.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The read failure.</param>
    /// <param name="tool">The tool's remote name.</param>
    [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "Left out the remote tool {Tool}: its schema could not be read")]
    internal static partial void UnreadableSchema(ILogger logger, Exception exception, string tool);
}
