namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Declares a method as a tool.
/// </summary>
/// <remarks>
/// <para>
/// What the method cannot say for itself. The prose a model reads comes from
/// <c>[Description]</c> and the schemas from the parameters and the return type; the name comes
/// from here, and only from here.
/// </para>
/// <para>
/// <see cref="ReadOnly"/> and <see cref="Destructive"/> are nullable rather than defaulted, so
/// that a validator can insist a tool state them. A default here would be the one nobody notices
/// inheriting, and a policy keyed on "destructive" is worth nothing if a tool can arrive without
/// having said - so an unstated <see cref="Destructive"/> is read as <c>true</c>.
/// </para>
/// </remarks>
/// <param name="name">The name a caller invokes this tool by.</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AgentToolAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name a caller invokes this tool by.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required, and a constructor argument so that leaving it out is a compile error rather
    /// than something a validator has to catch. A method name is a C# name; a tool name is part
    /// of a prompt shared with every other tool a model can see, including tools from other
    /// applications entirely. Deriving one from the other would be a guess, and a guess that
    /// looks like a convention is one that ships unnoticed.
    /// </para>
    /// <para>
    /// The same rule as <see cref="AgentToolDeclaration.Name"/>, which is <c>required</c> for
    /// the same reason. The two ways of declaring a tool should not disagree about whether
    /// naming it is deliberate.
    /// </para>
    /// </remarks>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets the name a person sees for this tool, or null to fall back to the name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool only reads.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool can destroy something a caller would not want destroyed.
    /// </summary>
    public bool Destructive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether calling this twice with the same arguments has the same effect as calling it once.
    /// </summary>
    /// <remarks>
    /// Follows <see cref="ReadOnly"/> when unset.
    /// </remarks>
    public bool Idempotent { get => this.idempotent ?? this.ReadOnly; set => this.idempotent = value; }

    /// <summary>
    /// Gets or sets a value indicating whether this tool reaches something outside the application.
    /// </summary>
    public bool OpenWorld { get; set; }

    /// <summary>
    /// Whether the tool is idempotent, where that was said.
    /// </summary>
    private bool? idempotent;
}
