using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools.Serialization;

/// <summary>
/// JSON representation of tool arguments, results and schemas.
/// </summary>
/// <remarks>
/// Schemas and payloads must use the same serializer options, otherwise property names disagree and
/// model-supplied fields are silently dropped; the options come from
/// <see cref="Microsoft.Extensions.AI.AIFunction.JsonSerializerOptions"/>. Nullability decides
/// required versus optional fields.
/// </remarks>
public static class AgentToolJson
{
    /// <summary>
    /// The default serializer options (<see cref="AIJsonUtilities.DefaultOptions"/>).
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions => AIJsonUtilities.DefaultOptions;

    /// <summary>
    /// Generated schemas by type and options.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type Type, JsonSerializerOptions Options), JsonElement> Cache = new();

    /// <summary>
    /// Gets the schema for a type.
    /// </summary>
    /// <typeparam name="T">The type to describe.</typeparam>
    /// <param name="serializerOptions">The options, or null for <see cref="DefaultSerializerOptions"/>.</param>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// Cached per type and options.
    /// </remarks>
    public static JsonElement Schema<T>(JsonSerializerOptions? serializerOptions = null)
    {
        return Schema(typeof(T), serializerOptions);
    }

    /// <summary>
    /// Gets the schema for a type.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <param name="serializerOptions">The options, or null for <see cref="DefaultSerializerOptions"/>.</param>
    /// <returns>The schema.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static JsonElement Schema(Type type, JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        var options = serializerOptions ?? DefaultSerializerOptions;

        return Cache.GetOrAdd(
            (type, options),
            key => AIJsonUtilities.CreateJsonSchema(type: key.Type, serializerOptions: key.Options));
    }
}
