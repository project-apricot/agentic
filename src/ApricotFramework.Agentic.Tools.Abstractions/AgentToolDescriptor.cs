using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool as held by this library: the function, its declaration and host metadata.
/// </summary>
/// <remarks>
/// The declaration sits beside the function rather than wrapping it, so a foreign
/// <see cref="AIFunction"/> stays reachable through <c>GetService()</c>. This is the unit
/// sources produce and filters and validators receive.
/// </remarks>
public sealed class AgentToolDescriptor
{
    /// <summary>
    /// Creates a new descriptor.
    /// </summary>
    /// <param name="tool">The function, or a declaration that is not invocable here.</param>
    /// <param name="declaration">The tool's declaration.</param>
    /// <param name="metadata">Host metadata, or null for none.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> or <paramref name="declaration"/> is null.</exception>
    public AgentToolDescriptor(AIFunctionDeclaration tool, IAgentToolDeclaration declaration, IEnumerable<object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(declaration);

        this.Tool = tool;
        this.Declaration = declaration;
        this.Metadata = metadata is null ? [] : [.. metadata];
    }

    /// <summary>
    /// Creates a descriptor for a tool that is its own declaration.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="metadata">Host metadata, or null for none.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public AgentToolDescriptor(AgentTool tool, IEnumerable<object>? metadata = null)
        : this(tool, tool, metadata)
    {
    }

    /// <summary>
    /// Gets the function, or a declaration of one this host cannot run.
    /// </summary>
    /// <remarks>
    /// Typed as a declaration so a catalogue may list a tool it cannot run; execution requires an
    /// <see cref="AIFunction"/> and refuses anything else.
    /// </remarks>
    public AIFunctionDeclaration Tool { get; }

    /// <summary>
    /// Gets the tool's declaration.
    /// </summary>
    /// <remarks>
    /// The tool itself where it is an <see cref="AgentTool"/>, otherwise an
    /// <see cref="AgentToolDeclaration"/> beside it.
    /// </remarks>
    public IAgentToolDeclaration Declaration { get; }

    /// <summary>
    /// Gets the host metadata attached to the tool.
    /// </summary>
    public IReadOnlyList<object> Metadata { get; }

    /// <summary>
    /// Gets the name the tool is offered under.
    /// </summary>
    /// <remarks>
    /// Taken from the declaration, which may differ from the function's own name (e.g. when prefixed).
    /// </remarks>
    public string Name => this.Declaration.Name;

    /// <summary>
    /// Gets the metadata of one type.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type.</typeparam>
    /// <returns>The matching metadata, in insertion order.</returns>
    public IReadOnlyList<TMetadata> GetMetadata<TMetadata>() => [.. this.Metadata.OfType<TMetadata>()];

    /// <summary>
    /// Gets the function, where this tool can be run here.
    /// </summary>
    /// <returns>The function, or null where only a declaration is held.</returns>
    public AIFunction? AsFunction() => this.Tool as AIFunction;

    /// <summary>
    /// Creates a descriptor for the same function with a different declaration.
    /// </summary>
    /// <param name="declaration">The new declaration.</param>
    /// <param name="metadata">Replacement metadata, or null to keep the existing metadata.</param>
    /// <returns>The new descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
    /// <remarks>
    /// Metadata, when given, replaces the existing metadata; use <see cref="WithMetadata"/> to append.
    /// </remarks>
    public AgentToolDescriptor With(IAgentToolDeclaration declaration, IEnumerable<object>? metadata = null) =>
        new(this.Tool, declaration, metadata ?? this.Metadata);

    /// <summary>
    /// Creates a descriptor with additional metadata appended.
    /// </summary>
    /// <param name="metadata">The metadata to append.</param>
    /// <returns>The new descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadata"/> is null.</exception>
    public AgentToolDescriptor WithMetadata(params object[] metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return new(this.Tool, this.Declaration, [.. this.Metadata, .. metadata]);
    }

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
