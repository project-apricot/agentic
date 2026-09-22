using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Options;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Adapters;

/// <summary>
/// A tool backed by an <see cref="AIFunction"/>.
/// </summary>
/// <remarks>
/// <para>
/// The adapter that makes the declaration model open. Anything expressible as an
/// <see cref="AIFunction"/> becomes a tool without a class being written for it - a delegate
/// wrapped by <c>AIFunctionFactory</c>, a function built from configuration, or a tool read off a
/// server the host happens to speak to.
/// </para>
/// <para>
/// That last one is worth naming, because it is not obvious: an MCP client's tool is an
/// <see cref="AIFunction"/>, so a foreign server's tools reach this model through the same adapter
/// as everything else. What they cannot bring with them is who may call them - a foreign tool
/// carries no authorization attribute - so that comes from whatever registered it.
/// </para>
/// <para>
/// Always <see cref="AgentToolResultKind.Whole"/>. A function returns one value.
/// </para>
/// </remarks>
public sealed class FunctionAgentTool : AgentTool
{
    /// <summary>
    /// The function behind the tool.
    /// </summary>
    private readonly AIFunction function;

    /// <summary>
    /// Creates a new instance of the tool.
    /// </summary>
    /// <param name="function">The function behind the tool.</param>
    /// <param name="options">What the function cannot say for itself.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public FunctionAgentTool(AIFunction function, AgentToolCreateOptions options)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(options);

        this.function = function;

        this.Name = options.Name ?? function.Name;
        this.Title = options.Title ?? this.Name;
        this.Description = options.Description ?? function.Description;
        this.IsReadOnly = options.IsReadOnly;
        this.IsDestructive = options.IsDestructive;
        this.IsIdempotent = options.IsIdempotent ?? options.IsReadOnly;
        this.IsOpenWorld = options.IsOpenWorld;
        this.OutputSchema = options.OutputSchema ?? function.ReturnJsonSchema;
        this.Labels = options.Labels ?? AgentToolLabels.None;
        this.SerializerOptions = options.SerializerOptions ?? function.JsonSerializerOptions;
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override string Title { get; }

    /// <inheritdoc />
    public override string Description { get; }

    /// <inheritdoc />
    public override bool IsReadOnly { get; }

    /// <inheritdoc />
    public override bool IsDestructive { get; }

    /// <inheritdoc />
    public override bool IsIdempotent { get; }

    /// <inheritdoc />
    public override bool IsOpenWorld { get; }

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> Labels { get; }

    /// <inheritdoc />
    public override JsonSerializerOptions SerializerOptions { get; }

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => AgentToolResultKind.Whole;

    /// <inheritdoc />
    public override JsonElement InputSchema => this.function.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? OutputSchema { get; }

    /// <summary>
    /// Gets the function behind this tool.
    /// </summary>
    /// <returns>The function.</returns>
    /// <remarks>
    /// For a host that needs what the adapter did not carry across - the protocol representation
    /// behind a foreign tool, say, or metadata in <see cref="AITool.AdditionalProperties"/>.
    /// </remarks>
    public AIFunction AsFunction()
    {
        return this.function;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeAsync(string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var arguments = this.Read(argumentsJson, context);

        yield return await this.function.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Turns the invocation into what the function expects.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON.</param>
    /// <param name="context">Who is asking.</param>
    /// <returns>The arguments.</returns>
    /// <remarks>
    /// The whole context is put into the function's own context bag as well as its parts, so a
    /// function that wants what this adapter has no way to project - a derived context, the
    /// caller - can reach for it.
    /// </remarks>
    private AIFunctionArguments Read(string? argumentsJson, AgentToolContext context)
    {
        Dictionary<string, object?>? values;

        var json = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;

        try
        {
            values = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, this.SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new AgentToolArgumentException("The arguments could not be read as an object.", exception);
        }

        return new AIFunctionArguments(values ?? [])
        {
            Services = context.Services,
            Context = new Dictionary<object, object?> { [typeof(AgentToolContext)] = context }
        };
    }
}
