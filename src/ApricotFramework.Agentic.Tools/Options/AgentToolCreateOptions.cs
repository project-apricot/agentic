using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApricotFramework.Agentic.Tools.Options;

/// <summary>
/// What a tool built at run time cannot work out for itself.
/// </summary>
/// <remarks>
/// A function knows its name, its prose, and its schemas. It does not know whether calling it
/// destroys anything, or what your organization labels it - and neither is guessable, so they are
/// stated here.
/// </remarks>
public class AgentToolCreateOptions
{
    /// <summary>
    /// Gets a value indicating whether the tool only reads.
    /// </summary>
    /// <remarks>
    /// Required rather than defaulted, for the same reason it is abstract on
    /// <see cref="AgentTool"/>: a default here would be the one nobody notices inheriting.
    /// </remarks>
    public required bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tool can destroy something a caller would not want destroyed.
    /// </summary>
    public required bool IsDestructive { get; init; }

    /// <summary>
    /// Gets a value indicating whether calling the tool twice has the same effect as calling it once.
    /// </summary>
    /// <remarks>
    /// Follows <see cref="IsReadOnly"/> when left unset.
    /// </remarks>
    public bool? IsIdempotent { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tool reaches something outside the application.
    /// </summary>
    public bool IsOpenWorld { get; init; }

    /// <summary>
    /// Gets the name to declare, or null to take the function's own.
    /// </summary>
    /// <remarks>
    /// Worth setting for a tool adapted from somewhere else, where the original name was chosen
    /// without knowing what else it would sit beside.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the title to declare, or null to fall back to the name.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the description to declare, or null to take the function's own.
    /// </summary>
    /// <remarks>
    /// Worth setting where the function's own prose was not written for the model that will read
    /// it - or where it came from somewhere outside the application, and is therefore text you
    /// did not write reaching a prompt you own.
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the schema of the complete result, or null to take the function's own.
    /// </summary>
    public JsonElement? OutputSchema { get; init; }

    /// <summary>
    /// Gets the host-defined labels the tool carries.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Labels { get; init; }

    /// <summary>
    /// Gets how the tool's arguments and result are read and written, or null to take the function's own.
    /// </summary>
    [JsonIgnore]
    public JsonSerializerOptions? SerializerOptions { get; init; }
}
