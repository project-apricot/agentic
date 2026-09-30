using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Invocation;

/// <summary>
/// A listed tool, as a function that goes back through the executor when called.
/// </summary>
/// <remarks>
/// <para>
/// What <see cref="IAgentToolExecutor.GetAvailableFunctionsAsync"/> hands a chat client. A
/// listing and a call are separated by however long a conversation takes, so the function cannot
/// hold the scope it was listed in - it carries the declaration and re-enters the executor,
/// which opens a fresh scope and rebuilds the context for each call.
/// </para>
/// <para>
/// That also means the filters run again at call time, which is the behavior worth having: a
/// tool listed twenty minutes ago and revoked since is refused rather than run.
/// </para>
/// </remarks>
internal sealed class ExecutorAgentTool : AgentTool
{
    /// <summary>
    /// What was listed.
    /// </summary>
    private readonly AgentToolDescriptor descriptor;

    /// <summary>
    /// Where the call goes.
    /// </summary>
    private readonly IAgentToolExecutor executor;

    /// <summary>
    /// Creates a new instance of the tool.
    /// </summary>
    /// <param name="descriptor">What was listed.</param>
    /// <param name="executor">Where the call goes.</param>
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
    /// <remarks>
    /// Forwarded so that a consumer reaching for what is really behind this - the underlying
    /// <c>McpClientTool</c>, say - finds it rather than finding a proxy.
    /// </remarks>
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
    /// Writes the arguments as the JSON the executor takes.
    /// </summary>
    /// <param name="arguments">The arguments as the caller built them.</param>
    /// <returns>The arguments as JSON.</returns>
    /// <remarks>
    /// A caller that already had JSON gets its own text back untouched, which is what keeps a
    /// large identifier intact through a round trip nobody asked for.
    /// </remarks>
    private string Write(AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.GetPayload() is { } payload
            ? payload.GetRawText()
            : JsonSerializer.Serialize<IDictionary<string, object?>>(arguments, this.JsonSerializerOptions);
    }

    /// <summary>
    /// Reads a result back as a value a chat client can carry.
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
