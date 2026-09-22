namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool as it was registered: the tool itself, and whatever was said about it.
/// </summary>
/// <remarks>
/// <para>
/// Immutable. Mutability belongs to the builder, which is what mirrors
/// <c>EndpointBuilder</c> giving way to <c>Endpoint</c>.
/// </para>
/// </remarks>
public sealed class AgentToolDescriptor
{
    /// <summary>
    /// Creates a new descriptor.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="metadata">What was said about it, or null for nothing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public AgentToolDescriptor(AgentTool tool, IEnumerable<object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(tool);

        this.Tool = tool;
        this.Metadata = metadata is null ? [] : [.. metadata];
    }

    /// <summary>
    /// Gets the tool.
    /// </summary>
    public AgentTool Tool { get; }

    /// <summary>
    /// Gets the name the tool is offered under.
    /// </summary>
    /// <remarks>
    /// The tool's own, forwarded so a registry can key on a descriptor without unwrapping it.
    /// </remarks>
    public string Name => this.Tool.Name;

    /// <summary>
    /// Gets everything said about the tool.
    /// </summary>
    public IReadOnlyList<object> Metadata { get; }

    /// <summary>
    /// Gets everything of one kind said about the tool.
    /// </summary>
    /// <typeparam name="TMetadata">The kind to look for.</typeparam>
    /// <returns>What was found, in the order it was added.</returns>
    public IReadOnlyList<TMetadata> GetMetadata<TMetadata>() => [.. this.Metadata.OfType<TMetadata>()];

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
