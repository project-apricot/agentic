using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool that can also report its result as it arrives.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="AIFunction"/>, not something adapted into one. That is what lets the same
/// declaration go into <c>ChatOptions.Tools</c> for an in-process loop and through
/// <c>McpServerTool.Create</c> for a remote protocol without an adapter in either direction -
/// local or remote is a property of the caller, not of the operation.
/// </para>
/// <para>
/// <strong>This is not how a tool is ordinarily written.</strong> An ordinary tool is a method
/// carrying <see cref="AgentToolAttribute"/>, or a function paired with an
/// <see cref="AgentToolDeclaration"/>; both take their argument binding and schema generation
/// from <c>AIFunctionFactory</c> rather than restating them. This type is what such a tool is
/// wrapped in, what a tool reached over a wire arrives as - and the escape hatch for the rare
/// tool that neither shape fits, which is the same position <see cref="AIFunction"/> itself and
/// the MCP SDK's <c>McpServerTool</c> occupy.
/// </para>
/// <para>
/// What deriving buys that a plain function cannot: <see cref="InvokeStreamingAsync"/>. A
/// function returns one value, so a tool whose result arrives as a sequence has to be one of
/// these.
/// </para>
/// <para>
/// Nothing here decides what a caller may see. Authorization is enforced by whatever runs the
/// call, so that a check a tool is trusted to perform is not a check a tool can forget.
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
    /// Follows <see cref="IsReadOnly"/> unless overridden, which is right for a read and has to
    /// be stated by any writing that is not idempotent.
    /// </remarks>
    public virtual bool IsIdempotent => this.IsReadOnly;

    /// <inheritdoc />
    public virtual bool IsOpenWorld => false;

    /// <inheritdoc />
    public abstract AgentToolResultKind ResultKind { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Read from <see cref="AgentToolLabelAttribute"/> unless a tool says otherwise.
    /// </remarks>
    public virtual IReadOnlyDictionary<string, object?> Labels => AgentToolLabels.ForType(this.GetType());

    /// <inheritdoc />
    /// <remarks>
    /// The declaration, in the terms the AI abstractions already use, so that a consumer which
    /// has never heard of this library still reads the behavior a policy here reads.
    /// </remarks>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => field ??= AgentToolAnnotations.For(this);

    /// <summary>
    /// Runs the tool, reporting its result as it arrives.
    /// </summary>
    /// <param name="arguments">The arguments carrying the invocation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, as one item for a whole result or as a sequence of them.</returns>
    /// <remarks>
    /// The extra path a tool gets for being one of ours. <see cref="AIFunction.InvokeAsync"/>
    /// returns one value and every ecosystem consumer wants that, so a sequence tool collects
    /// there and streams here - which is why a consumer that cannot stream pays nothing for a
    /// tool that can.
    /// </remarks>
    public virtual async IAsyncEnumerable<object?> InvokeStreamingAsync(
        AIFunctionArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await this.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
