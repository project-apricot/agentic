using System.Text.Json;
using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Serialization;

/// <summary>
/// Reads the arguments a caller supplied for a tool.
/// </summary>
internal static class AgentToolArguments
{
    /// <summary>
    /// Reads the arguments as the type a tool expects.
    /// </summary>
    /// <typeparam name="TArguments">The type the tool expects.</typeparam>
    /// <param name="argumentsJson">The arguments as JSON.</param>
    /// <param name="serializerOptions">How to read them.</param>
    /// <returns>The arguments.</returns>
    /// <exception cref="AgentToolArgumentException">Thrown when the arguments cannot be read.</exception>
    /// <remarks>
    /// An absent payload is read as an empty object rather than rejected. A model calling a tool
    /// that takes nothing may well send nothing, and refusing that would be a failure it cannot
    /// act on.
    /// </remarks>
    internal static TArguments Read<TArguments>(string? argumentsJson, JsonSerializerOptions serializerOptions)
    {
        var json = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;

        TArguments? arguments;

        try
        {
            arguments = JsonSerializer.Deserialize<TArguments>(json, serializerOptions);
        }
        catch (JsonException exception)
        {
            throw new AgentToolArgumentException("The arguments could not be read as the shape this tool declares.", exception);
        }

        return arguments ?? throw new AgentToolArgumentException("The arguments were read as null, where this tool declares a value.");
    }
}
