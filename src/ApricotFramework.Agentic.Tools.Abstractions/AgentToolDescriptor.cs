using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool as this library holds it: the function, what is declared about it, and what a host said.
/// </summary>
/// <remarks>
/// <para>
/// Three things rather than one, because only the first of them is always ours. A tool read off a
/// foreign server is an <see cref="AIFunction"/> that already exists and that a consumer may want
/// to reach through <c>GetService()</c>; putting the declaration beside it rather than
/// wrapping it keeps that reachable, and makes renaming a foreign tool a new declaration rather
/// than another object in the way.
/// </para>
/// <para>
/// This is the unit sources hand over and the one filters and validators are given, so a policy
/// written against a declaration applies to a tool whoever wrote it.
/// </para>
/// </remarks>
public sealed class AgentToolDescriptor
{
    /// <summary>
    /// Creates a new descriptor.
    /// </summary>
    /// <param name="tool">The function, or a declaration of one that is not invocable here.</param>
    /// <param name="declaration">What is declared about it.</param>
    /// <param name="metadata">What a host said about it, or null for nothing.</param>
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
    /// Creates a descriptor for a tool that declares everything about itself.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="metadata">What a host said about it, or null for nothing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is null.</exception>
    public AgentToolDescriptor(AgentTool tool, IEnumerable<object>? metadata = null)
        : this(tool, tool, metadata)
    {
    }

    /// <summary>
    /// Gets the function, or a declaration of one this host cannot run.
    /// </summary>
    /// <remarks>
    /// Typed as the declaration rather than as <see cref="AIFunction"/> so that a catalogue can
    /// list a tool it has no way to invoke. Whatever runs a call requires an
    /// <see cref="AIFunction"/> and refuses what is not one, rather than every listing having to
    /// pretend it could.
    /// </remarks>
    public AIFunctionDeclaration Tool { get; }

    /// <summary>
    /// Gets what is declared about the tool.
    /// </summary>
    /// <remarks>
    /// The tool itself where it is an <see cref="AgentTool"/>, and an
    /// <see cref="AgentToolDeclaration"/> beside it where it is not. The reading side cannot tell
    /// the difference, which is the point.
    /// </remarks>
    public IAgentToolDeclaration Declaration { get; }

    /// <summary>
    /// Gets everything a host said about the tool.
    /// </summary>
    /// <remarks>
    /// Untyped, in the shape of <c>EndpointMetadataCollection</c> and
    /// <c>IMcpServerPrimitive.Metadata</c>, because a host says things this library has never
    /// heard of. Typed readers sit on top.
    /// </remarks>
    public IReadOnlyList<object> Metadata { get; }

    /// <summary>
    /// Gets the name the tool is offered under.
    /// </summary>
    /// <remarks>
    /// The declaration's, which is not always the function's: a foreign tool offered under a
    /// prefixed name keeps its own inside.
    /// </remarks>
    public string Name => this.Declaration.Name;

    /// <summary>
    /// Gets everything of one kind said about the tool.
    /// </summary>
    /// <typeparam name="TMetadata">The kind to look for.</typeparam>
    /// <returns>What was found, in the order it was added.</returns>
    public IReadOnlyList<TMetadata> GetMetadata<TMetadata>() => [.. this.Metadata.OfType<TMetadata>()];

    /// <summary>
    /// Gets the function, where this tool can be run here.
    /// </summary>
    /// <returns>The function, or null where only a declaration is held.</returns>
    public AIFunction? AsFunction() => this.Tool as AIFunction;

    /// <summary>
    /// Says the same thing about the same tool, differently.
    /// </summary>
    /// <param name="declaration">What to say instead.</param>
    /// <param name="metadata">What a host says about it, or null to keep what was said.</param>
    /// <returns>The new descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="declaration"/> is null.</exception>
    /// <remarks>
    /// How curation prefixes a name or pins a description: the function is untouched and only
    /// what is claimed about it changes.
    /// </remarks>
    public AgentToolDescriptor With(IAgentToolDeclaration declaration, IEnumerable<object>? metadata = null) =>
        new(this.Tool, declaration, metadata ?? this.Metadata);

    /// <inheritdoc />
    public override string ToString() => this.Name;
}
