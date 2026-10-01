namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A declaration for a tool that is not an <see cref="AgentTool"/>.
/// </summary>
/// <remarks>
/// For delegates, foreign MCP tools or remote descriptions; also how a host restates a tool it did
/// not write (e.g. a prefixed name) without wrapping the function.
/// </remarks>
public sealed record AgentToolDeclaration : IAgentToolDeclaration
{
    /// <summary>
    /// The stated idempotency, if any.
    /// </summary>
    private readonly bool? idempotent;

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Falls back to <see cref="Name"/>.
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
    /// Required so no tool arrives without stating it.
    /// </remarks>
    public required bool IsReadOnly { get; init; }

    /// <inheritdoc />
    public required bool IsDestructive { get; init; }

    /// <inheritdoc />
    /// <remarks>
    /// Defaults to <see cref="IsReadOnly"/>; set it for writes that are not idempotent.
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
    /// The registry rejects <see cref="AgentToolResultKind.Sequence"/> here; streaming requires an
    /// <see cref="AgentTool"/>.
    /// </remarks>
    public AgentToolResultKind ResultKind { get; init; } = AgentToolResultKind.Whole;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Labels { get; init; } = AgentToolLabels.None;

    /// <summary>
    /// Copies a declaration as a record, for use with <c>with</c>.
    /// </summary>
    /// <param name="declaration">The declaration to copy.</param>
    /// <returns>The declaration as a record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
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
