using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// Reading a call's arguments as a declared shape.
/// </summary>
/// <remarks>
/// What a host writing against <see cref="AgentTool"/> by hand has to do for itself, and what
/// <c>AIFunctionFactory</c> does for a method-based tool. Prefers the caller's own JSON where
/// there is any, because nothing has rounded a number in it.
/// </remarks>
internal static class ProbeArgumentReader
{
    /// <summary>Reads the arguments.</summary>
    /// <typeparam name="TArguments">The declared shape.</typeparam>
    /// <param name="arguments">The arguments as they arrived.</param>
    /// <param name="serializerOptions">How to read them.</param>
    /// <returns>The arguments.</returns>
    internal static TArguments Read<TArguments>(AIFunctionArguments arguments, JsonSerializerOptions serializerOptions)
    {
        var payload = arguments.GetPayload()
                      ?? JsonSerializer.SerializeToElement<IDictionary<string, object?>>(arguments, serializerOptions);

        try
        {
            return payload.Deserialize<TArguments>(serializerOptions)
                   ?? throw new AgentToolArgumentException("The arguments were read as null.");
        }
        catch (JsonException exception)
        {
            throw new AgentToolArgumentException("The arguments could not be read as the shape this tool declares.", exception);
        }
    }
}
