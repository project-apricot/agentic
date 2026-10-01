using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool that can also stream its result.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="AIFunction"/>, so it works with <c>ChatOptions.Tools</c> and
/// <c>McpServerTool.Create</c> without adapters. Ordinary tools are written with
/// <see cref="AgentToolAttribute"/> or an <see cref="AgentToolDeclaration"/>; derive from this only
/// when neither fits nor the result must stream via <see cref="InvokeStreamingAsync"/>.
/// </para>
/// <para>
/// Authorization is enforced by whatever runs the call, not by the tool.
/// </para>
/// </remarks>
public abstract class AgentTool : AIFunction, IAgentToolDeclaration
{
    /// <inheritdoc />
    public abstract override string Name { get; }

    /// <inheritdoc />
    public abstract string Title { get; }

    /// <inheritdoc />
    public abstract override string Description { get; }

    /// <inheritdoc />
    public abstract bool IsReadOnly { get; }

    /// <inheritdoc />
    public abstract bool IsDestructive { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Defaults to <see cref="IsReadOnly"/>; override for writes that are not idempotent.
    /// </remarks>
    public virtual bool IsIdempotent => this.IsReadOnly;

    /// <inheritdoc />
    public virtual bool IsOpenWorld => false;

    /// <inheritdoc />
    public abstract AgentToolResultKind ResultKind { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Read from <see cref="AgentToolLabelAttribute"/> by default.
    /// </remarks>
    public virtual IReadOnlyDictionary<string, object?> Labels => AgentToolLabels.ForType(this.GetType());

    /// <inheritdoc />
    /// <remarks>
    /// The declaration projected as MCP-style annotations, so consumers unaware of this library can read it.
    /// </remarks>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => field ??= AgentToolAnnotations.For(this);

    /// <summary>
    /// Runs the tool, streaming its result.
    /// </summary>
    /// <param name="arguments">The invocation arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, as a single item or a sequence.</returns>
    /// <remarks>
    /// <see cref="AIFunction.InvokeAsync"/> returns the collected result; this is the streaming path.
    /// </remarks>
    public virtual async IAsyncEnumerable<object?> InvokeStreamingAsync(AIFunctionArguments arguments, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await this.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
