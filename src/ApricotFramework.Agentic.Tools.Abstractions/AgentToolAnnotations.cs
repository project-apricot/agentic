using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Maps a declaration to and from MCP annotation names in <see cref="AITool.AdditionalProperties"/>.
/// </summary>
public static class AgentToolAnnotations
{
    /// <summary>
    /// The human-readable name.
    /// </summary>
    public const string Title = "title";

    /// <summary>
    /// Whether the tool only reads.
    /// </summary>
    public const string ReadOnlyHint = "readOnlyHint";

    /// <summary>
    /// Whether the tool may be destructive.
    /// </summary>
    public const string DestructiveHint = "destructiveHint";

    /// <summary>
    /// Whether repeated calls have no further effect.
    /// </summary>
    public const string IdempotentHint = "idempotentHint";

    /// <summary>
    /// Whether the tool reaches outside the application.
    /// </summary>
    public const string OpenWorldHint = "openWorldHint";

    /// <summary>
    /// Projects a declaration into AI tool properties.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="labels">Whether to include the host's labels.</param>
    /// <returns>The properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
    /// <remarks>
    /// Annotations win over labels with the same name.
    /// </remarks>
    public static IReadOnlyDictionary<string, object?> For(IAgentToolDeclaration declaration, bool labels = true)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (labels)
        {
            foreach (var label in declaration.Labels)
            {
                properties[label.Key] = label.Value;
            }
        }

        properties[Title] = declaration.Title;
        properties[ReadOnlyHint] = declaration.IsReadOnly;
        properties[DestructiveHint] = declaration.IsDestructive;
        properties[IdempotentHint] = declaration.IsIdempotent;
        properties[OpenWorldHint] = declaration.IsOpenWorld;

        return properties;
    }

    /// <summary>
    /// Reads the annotations a function already carries.
    /// </summary>
    /// <param name="function">The function to read.</param>
    /// <returns>The annotations, null where not stated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
    /// <remarks>
    /// A missing hint differs from <c>false</c>: a tool silent on destructiveness should be assumed
    /// destructive, which is the caller's decision.
    /// </remarks>
    public static (string? Title, bool? IsReadOnly, bool? IsDestructive, bool? IsIdempotent, bool? IsOpenWorld) Read(AITool function)
    {
        ArgumentNullException.ThrowIfNull(function);

        var properties = function.AdditionalProperties;

        return (
            properties.TryGetValue(Title, out var title) ? title as string : null,
            Flag(properties, ReadOnlyHint),
            Flag(properties, DestructiveHint),
            Flag(properties, IdempotentHint),
            Flag(properties, OpenWorldHint));

        static bool? Flag(IReadOnlyDictionary<string, object?> properties, string name) =>
            properties.TryGetValue(name, out var value) && value is bool flag ? flag : null;
    }
}
