namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Declares a method as a tool.
/// </summary>
/// <remarks>
/// The description comes from <c>[Description]</c> and the schemas from the signature; the name
/// comes only from here. Unstated, <see cref="ReadOnly"/> and <see cref="Destructive"/> are <c>false</c>.
/// </remarks>
/// <param name="name">The name a caller invokes this tool by.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AgentToolAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name a caller invokes this tool by.
    /// </summary>
    /// <remarks>
    /// Required and never derived from the method name, matching <see cref="AgentToolDeclaration.Name"/>.
    /// </remarks>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets the human-readable name, or null to fall back to <see cref="Name"/>.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool only reads.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool may perform destructive changes.
    /// </summary>
    public bool Destructive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether repeated calls with the same arguments have no further effect.
    /// </summary>
    /// <remarks>
    /// Follows <see cref="ReadOnly"/> when unset.
    /// </remarks>
    public bool Idempotent { get => this.idempotent ?? this.ReadOnly; set => this.idempotent = value; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool reaches outside the application.
    /// </summary>
    public bool OpenWorld { get; set; }

    /// <summary>
    /// The stated idempotency, if any.
    /// </summary>
    private bool? idempotent;
}
