namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Attaches a host-defined label to a tool.
/// </summary>
/// <param name="name">What the label is called.</param>
/// <param name="value">What it says, or null where the name alone is the statement.</param>
/// <remarks>
/// <para>
/// The mechanism is here; the vocabulary is not, and deliberately. Whether tools should be marked
/// by the sensitivity of the data they expose, the tier of caller allowed to reach them, or
/// something nobody has thought of yet is a question about one organization. A library that
/// answered it would be wrong for the next one.
/// </para>
/// <para>
/// What a host does with a label is its own business too. An authorization handler can narrow on
/// one, since the tool is passed as the authorization resource; an
/// <c>IAgentToolValidator</c> can insist every tool carries one and reject at startup the
/// ones that do not; a surface can decide what to advertise from them.
/// </para>
/// <code>
/// [AgentToolLabel("sensitivity", Sensitivity.Personal)]
/// [Authorize(Policy = "anything")]
/// </code>
/// <para>
/// Applied to a holding class or a base class, it labels a family of tools, and a method or a
/// derived tool restating the same name overrides it rather than adding a second.
/// </para>
/// <para>
/// <strong>Not sealed, on purpose.</strong> A host that wants its vocabulary closed at the point
/// of declaration derives from this, and gets a label name it cannot typo and a value that has
/// to be one of the type's:
/// </para>
/// <code>
/// [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
/// public sealed class SensitivityAttribute(Sensitivity level)
///     : AgentToolLabelAttribute(SupportDeskLabels.Sensitivity, level)
/// {
///     public Sensitivity Level { get; } = level;
/// }
/// </code>
/// <para>
/// Such an attribute is read both ways at once, which is the reason to derive rather than to
/// write an unrelated one: a portable filter finds it through
/// <see cref="AgentToolLabels.TryGetLabel{TValue}"/> without knowing the type exists, and the
/// host's own handler finds it in <see cref="AgentToolDescriptor.Metadata"/> as itself.
/// </para>
/// <para>
/// Prefer the label when reading it. <strong>Labels override by name; metadata accumulates</strong> -
/// so a method narrowing its family's label reads correctly as a label, while the metadata holds
/// both attributes in the order they were said, the holding class first.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AgentToolLabelAttribute(string name, object? value = null) : Attribute
{
    /// <summary>
    /// Gets what the label is called.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets what the label says.
    /// </summary>
    public object? Value { get; } = value;
}
