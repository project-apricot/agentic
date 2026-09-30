using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Adapters;

/// <summary>
/// A tool declared once and built again for every call, from the caller's own scope.
/// </summary>
/// <remarks>
/// <para>
/// What a class-per-tool registration becomes. The declaration - a name, a title, prose, and two
/// schemas - is read once from an instance built at composition, because none of them varies by
/// caller. The instance that actually runs is resolved from the scope the call opened, which is
/// what lets a tool take a scoped dependency through its constructor the way an endpoint does.
/// </para>
/// </remarks>
public sealed class ScopedAgentTool : AgentTool
{
    /// <summary>
    /// What the tool says about itself.
    /// </summary>
    private readonly AgentToolDeclaration declaration;

    /// <summary>
    /// Creates a new instance of the tool.
    /// </summary>
    /// <param name="toolType">The type to resolve for each call.</param>
    /// <param name="snapshot">An instance, read for what it declares and then discarded.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
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
    /// Gets the type built for each call.
    /// </summary>
    /// <remarks>
    /// A tool written as a class is no longer reachable by its own type from a listing, because
    /// no instance of it exists until somebody calls it. This is what is left of that, for a
    /// diagnostic or a host that keyed something on it - though narrowing on what a tool
    /// declares, or on what the host said about it, is the better habit and works for a tool
    /// that never had a class.
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
    /// Builds the tool for one call.
    /// </summary>
    /// <param name="arguments">The arguments carrying the scope.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the call carries no scope.</exception>
    private AgentTool Resolve(AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var services = arguments.Services ?? throw new InvalidOperationException(
            $"The tool '{this.Name}' is resolved per call and the call carried no services. " +
            "Run it through IAgentToolExecutor.");

        return (AgentTool)ActivatorUtilities.GetServiceOrCreateInstance(services, this.ToolType);
    }
}
