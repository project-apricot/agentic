namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Attaches a host-defined label to a tool.
/// </summary>
/// <param name="name">The label name.</param>
/// <param name="value">The label value, or null where the name alone is the statement.</param>
/// <remarks>
/// <para>
/// The library defines no label vocabulary; that is the host's. On a holding or base class it
/// labels a family of tools, and a method or derived tool restating the same name overrides it.
/// </para>
/// <para>
/// Not sealed so a host can derive a typed attribute that closes its vocabulary; such an attribute
/// is found both by <see cref="AgentToolLabels.TryGetLabel{TValue}"/> and as itself in
/// <see cref="AgentToolDescriptor.Metadata"/>. Labels override by name whereas metadata
/// accumulates (holding class first), so prefer reading the label.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AgentToolLabelAttribute(string name, object? value = null) : Attribute
{
    /// <summary>
    /// Gets the label name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the label value.
    /// </summary>
    public object? Value { get; } = value;
}
