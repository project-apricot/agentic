using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Adapters;

/// <summary>
/// A class-per-tool whose declaration is read once and whose instance is resolved per call.
/// </summary>
/// <remarks>
/// The declaration is read from a snapshot instance at composition; the running instance comes from
/// the call's scope, so tools can take scoped constructor dependencies.
/// </remarks>
public sealed class ScopedAgentTool : AgentTool
{
    /// <summary>
    /// The tool's declaration.
    /// </summary>
    private readonly AgentToolDeclaration declaration;

    /// <summary>
    /// Creates the adapter.
    /// </summary>
    /// <param name="toolType">The type to resolve for each call.</param>
    /// <param name="snapshot">An instance read for its declaration, then discarded.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public ScopedAgentTool(Type toolType, AgentTool snapshot)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        ArgumentNullException.ThrowIfNull(snapshot);

        this.ToolType = toolType;
        this.declaration = AgentToolDeclaration.From(snapshot);
        this.JsonSchema = snapshot.JsonSchema;
        this.ReturnJsonSchema = snapshot.ReturnJsonSchema;
        this.JsonSerializerOptions = snapshot.JsonSerializerOptions;
    }

    /// <summary>
    /// Gets the tool type resolved for each call.
    /// </summary>
    /// <remarks>
    /// Mainly for diagnostics; prefer matching on the declaration or metadata, which also works for
    /// tools without a class.
    /// </remarks>
    public Type ToolType { get; }

    /// <inheritdoc />
    public override string Name => this.declaration.Name;

    /// <inheritdoc />
    public override string Title => this.declaration.Title;

    /// <inheritdoc />
    public override string Description => this.declaration.Description;

    /// <inheritdoc />
    public override bool IsReadOnly => this.declaration.IsReadOnly;

    /// <inheritdoc />
    public override bool IsDestructive => this.declaration.IsDestructive;

    /// <inheritdoc />
    public override bool IsIdempotent => this.declaration.IsIdempotent;

    /// <inheritdoc />
    public override bool IsOpenWorld => this.declaration.IsOpenWorld;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => this.declaration.ResultKind;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> Labels => this.declaration.Labels;

    /// <inheritdoc />
    public override JsonElement JsonSchema { get; }

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema { get; }

    /// <inheritdoc />
    public override JsonSerializerOptions JsonSerializerOptions { get; }

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        return Resolve(arguments).InvokeAsync(arguments, cancellationToken);
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<object?> InvokeStreamingAsync(AIFunctionArguments arguments, CancellationToken cancellationToken = default)
    {
        return Resolve(arguments).InvokeStreamingAsync(arguments, cancellationToken);
    }

    /// <summary>
    /// Resolves the tool for one call.
    /// </summary>
    /// <param name="arguments">The arguments carrying the scope.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="InvalidOperationException">The call carries no services.</exception>
    private AgentTool Resolve(AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var services = arguments.Services ?? throw new InvalidOperationException(
            $"The tool '{this.Name}' is resolved per call and the call carried no services. " +
            "Run it through IAgentToolExecutor.");

        return (AgentTool)ActivatorUtilities.GetServiceOrCreateInstance(services, this.ToolType);
    }
}
