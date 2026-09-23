using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>
/// A tool written by hand against <see cref="AgentTool"/>.
/// </summary>
/// <remarks>
/// The escape hatch rather than the ordinary way to write a tool - an ordinary tool is a method.
/// These tests need a type because what they are about is the attributes a type carries, and a
/// method-based tool reads its attributes from the method and its holder instead.
/// </remarks>
public abstract class ProbeToolBase : AgentTool
{
    /// <summary>
    /// A tool that takes nothing.
    /// </summary>
    private static readonly JsonElement NoArguments = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Does a thing.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => AgentToolResultKind.Whole;

    /// <inheritdoc />
    public override JsonElement JsonSchema => NoArguments;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => AgentToolJson.Schema<string>();

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        ValueTask.FromResult<object?>("ok");
}
