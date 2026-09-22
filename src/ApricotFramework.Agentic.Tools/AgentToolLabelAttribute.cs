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
/// <see cref="IAgentToolValidator"/> can insist every tool carries one and reject at startup the
/// ones that do not; a surface can decide what to advertise from them.
/// </para>
/// <code>
/// [AgentToolLabel("sensitivity", Sensitivity.Personal)]
/// [AuthorizeAnyAccess("anything")]
/// </code>
/// <para>
/// Applied to a base class, it labels a family of tools, and a derived tool restating the same
/// name overrides it rather than adding a second.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AgentToolLabelAttribute(string name, object? value = null) : Attribute
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
