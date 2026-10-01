using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// A listed tool exposed as a function that calls back through the executor.
/// </summary>
/// <remarks>Holds no scope: each call re-enters <see cref="IAgentToolExecutor"/> with a fresh scope and context, so filters run again and a since-revoked tool is refused.</remarks>
internal sealed class ExecutorAgentTool : AgentTool
{
    /// <summary>
    /// The listed tool.
    /// </summary>
    private readonly AgentToolDescriptor descriptor;

    /// <summary>
    /// Where calls go.
    /// </summary>
    private readonly IAgentToolExecutor executor;

    /// <summary>
    /// Creates the tool.
    /// </summary>
    /// <param name="descriptor">The listed tool.</param>
    /// <param name="executor">Where calls go.</param>
    internal ExecutorAgentTool(AgentToolDescriptor descriptor, IAgentToolExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(executor);

        this.descriptor = descriptor;
        this.executor = executor;
    }

    /// <inheritdoc />
    public override string Name => this.descriptor.Declaration.Name;

    /// <inheritdoc />
    public override string Title => this.descriptor.Declaration.Title;

    /// <inheritdoc />
    public override string Description => this.descriptor.Declaration.Description;

    /// <inheritdoc />
    public override bool IsReadOnly => this.descriptor.Declaration.IsReadOnly;

    /// <inheritdoc />
    public override bool IsDestructive => this.descriptor.Declaration.IsDestructive;

    /// <inheritdoc />
    public override bool IsIdempotent => this.descriptor.Declaration.IsIdempotent;

    /// <inheritdoc />
    public override bool IsOpenWorld => this.descriptor.Declaration.IsOpenWorld;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => this.descriptor.Declaration.ResultKind;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> Labels => this.descriptor.Declaration.Labels;

    /// <inheritdoc />
    public override JsonElement JsonSchema => this.descriptor.Tool.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => this.descriptor.Tool.ReturnJsonSchema;

    /// <inheritdoc />
    /// <remarks>Forwards to the underlying tool so consumers can reach e.g. the <c>McpClientTool</c>.</remarks>
    public override object? GetService(Type serviceType, object? serviceKey = null) => base.GetService(serviceType, serviceKey) ?? this.descriptor.Tool.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var result = await this.executor.InvokeCompleteAsync(this.Name, Write(arguments), cancellationToken).ConfigureAwait(false);

        return Read(result);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeStreamingAsync(AIFunctionArguments arguments, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in this.executor.InvokeAsync(this.Name, Write(arguments), cancellationToken).ConfigureAwait(false))
        {
            yield return Read(chunk);
        }
    }

    /// <summary>
    /// Serializes the arguments to JSON for the executor.
    /// </summary>
    /// <param name="arguments">The caller's arguments.</param>
    /// <returns>The arguments as JSON.</returns>
    /// <remarks>Arguments that are already JSON are returned untouched, preserving large identifiers.</remarks>
    private string Write(AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.GetPayload() is { } payload
            ? payload.GetRawText()
            : JsonSerializer.Serialize<IDictionary<string, object?>>(arguments, this.JsonSerializerOptions);
    }

    /// <summary>
    /// Parses a JSON result into a value a chat client can carry.
    /// </summary>
    /// <param name="json">The result as JSON.</param>
    /// <returns>The result.</returns>
    private static JsonElement Read(string json)
    {
        // cloned, so the element outlives the document it was read from
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
