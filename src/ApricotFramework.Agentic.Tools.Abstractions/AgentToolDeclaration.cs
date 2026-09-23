namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// What is said about a tool that is not one of this library's classes.
/// </summary>
/// <remarks>
/// For a function built from a delegate, read off a foreign MCP server, or described by a remote
/// service over the wire - none of which can carry an attribute or override a property. It is also
/// how a host says something different about a tool it did not write: a prefixed name or a
/// description it pinned is a new declaration over the same function, not a wrapper around it.
/// </remarks>
public sealed record AgentToolDeclaration : IAgentToolDeclaration
{
    /// <summary>
    /// Whether the tool is idempotent, where that was said.
    /// </summary>
    private readonly bool? idempotent;

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Falls back to the name. A title is what a person reads, and a name is better than nothing.
    /// </remarks>
    public string Title
    {
        get => field ?? this.Name;
        init;
    }

    /// <inheritdoc />
    public string Description { get; init; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// Required rather than defaulted, for the same reason it is abstract on <see cref="AgentTool"/>:
    /// a default would be the one nobody notices inheriting, and a policy keyed on it is worth
    /// nothing if a tool can arrive without having said.
    /// </remarks>
    public required bool IsReadOnly { get; init; }

    /// <inheritdoc />
    public required bool IsDestructive { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Follows <see cref="IsReadOnly"/> unless stated, which is right for a read and has to be
    /// said by any writing that is not idempotent.
    /// </remarks>
    public bool IsIdempotent
    {
        get => this.idempotent ?? this.IsReadOnly;
        init => this.idempotent = value;
    }

    /// <inheritdoc />
    public bool IsOpenWorld { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// A function returns one value, so anything built from one is <see cref="AgentToolResultKind.Whole"/>,
    /// and the registry refuses a declaration that claims otherwise. A result that arrives as a
    /// sequence needs an <see cref="AgentTool"/>, which is the only thing that can produce items.
    /// </remarks>
    public AgentToolResultKind ResultKind { get; init; } = AgentToolResultKind.Whole;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Labels { get; init; } = AgentToolLabels.None;

    /// <summary>
    /// Copies a declaration so that one thing about it can be said differently.
    /// </summary>
    /// <param name="declaration">What is already said.</param>
    /// <returns>The same, as a record, for <c>with</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
    /// <remarks>
    /// How curation renames a foreign tool or pins prose it did not write, without an object
    /// around the function.
    /// </remarks>
    public static AgentToolDeclaration From(IAgentToolDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        return declaration as AgentToolDeclaration ?? new AgentToolDeclaration
        {
            Name = declaration.Name,
            Title = declaration.Title,
            Description = declaration.Description,
            IsReadOnly = declaration.IsReadOnly,
            IsDestructive = declaration.IsDestructive,
            IsIdempotent = declaration.IsIdempotent,
            IsOpenWorld = declaration.IsOpenWorld,
            ResultKind = declaration.ResultKind,
            Labels = declaration.Labels
        };
    }

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
