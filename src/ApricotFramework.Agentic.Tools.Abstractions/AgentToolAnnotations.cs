using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A declaration, said in the way the AI abstractions already say it.
/// </summary>
/// <remarks>
/// <see cref="AITool.AdditionalProperties"/> is where the ecosystem keeps a tool's annotations,
/// and these are the names the Model Context Protocol gives them. Projecting a declaration into
/// it means a consumer that has never heard of this library - <c>McpServerTool.Create</c>, a chat
/// client rendering a confirmation prompt - reads the same behavior a filter here reads.
/// </remarks>
public static class AgentToolAnnotations
{
    /// <summary>
    /// The name a person sees.
    /// </summary>
    public const string Title = "title";

    /// <summary>
    /// Whether the tool only reads.
    /// </summary>
    public const string ReadOnlyHint = "readOnlyHint";

    /// <summary>
    /// Whether the tool can destroy something.
    /// </summary>
    public const string DestructiveHint = "destructiveHint";

    /// <summary>
    /// Whether calling twice is the same as calling once.
    /// </summary>
    public const string IdempotentHint = "idempotentHint";

    /// <summary>
    /// Whether the tool reaches outside the application.
    /// </summary>
    public const string OpenWorldHint = "openWorldHint";

    /// <summary>
    /// Projects a declaration into the properties an AI tool carries.
    /// </summary>
    /// <param name="declaration">What was declared.</param>
    /// <param name="labels">Whether to include the host's labels under their own names.</param>
    /// <returns>The properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
    /// <remarks>
    /// Labels are included by default, under the names the host chose. A host whose label names
    /// could collide with an annotation name has said something ambiguous, and the annotation wins.
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
    /// Reads whatever a function already says about itself in these terms.
    /// </summary>
    /// <param name="function">The function to read.</param>
    /// <returns>What it said, with anything unsaid left to the caller to decide.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Deliberately returns nullables rather than a declaration. A missing hint is different from
    /// a declared <c>false</c>, and the difference is the one that matters: a tool that did not
    /// say whether it destroys anything should be assumed to, and only the caller can decide that.
    /// </para>
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
