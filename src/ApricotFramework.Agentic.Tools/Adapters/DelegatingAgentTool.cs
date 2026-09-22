using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Adapters;

/// <summary>
/// An optional base for a tool that passes through to another, changing some of what it declares.
/// </summary>
/// <remarks>
/// <para>
/// For a tool a host did not write and cannot edit. A foreign server names its tools without
/// knowing what they will sit beside, describes them in prose written for somebody else's model,
/// and may say nothing at all about whether calling one destroys anything. Wrapping is how a host
/// takes responsibility for a declaration it inherited.
/// </para>
/// <para>
/// Every member delegates, so a wrapper says only what it changes. See
/// <see cref="RenamedAgentTool"/> for the one that comes up most.
/// </para>
/// </remarks>
/// <param name="inner">The tool to pass through to.</param>
public abstract class DelegatingAgentTool(AgentTool inner) : AgentTool
{
    /// <summary>
    /// Gets the tool being passed through to.
    /// </summary>
    protected AgentTool Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public override string Name => this.Inner.Name;

    /// <inheritdoc />
    public override string Title => this.Inner.Title;

    /// <inheritdoc />
    public override string Description => this.Inner.Description;

    /// <inheritdoc />
    public override bool IsReadOnly => this.Inner.IsReadOnly;

    /// <inheritdoc />
    public override bool IsDestructive => this.Inner.IsDestructive;

    /// <inheritdoc />
    public override bool IsIdempotent => this.Inner.IsIdempotent;

    /// <inheritdoc />
    public override bool IsOpenWorld => this.Inner.IsOpenWorld;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> Labels => this.Inner.Labels;

    /// <inheritdoc />
    public override JsonSerializerOptions SerializerOptions => this.Inner.SerializerOptions;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => this.Inner.ResultKind;

    /// <inheritdoc />
    public override JsonElement InputSchema => this.Inner.InputSchema;

    /// <inheritdoc />
    public override JsonElement? OutputSchema => this.Inner.OutputSchema;

    /// <inheritdoc />
    public override IAsyncEnumerable<object?> InvokeAsync(string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
    {
        return this.Inner.InvokeAsync(argumentsJson, context, cancellationToken);
    }
}
