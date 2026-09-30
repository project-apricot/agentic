using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// A tool written by hand against <see cref="AgentTool"/>, with typed arguments and result.
/// </summary>
/// <typeparam name="TArguments">What the caller supplies.</typeparam>
/// <typeparam name="TResult">What the tool returns.</typeparam>
/// <remarks>
/// <para>
/// Test infrastructure, not a shipped shape. An ordinary tool is a method carrying
/// <see cref="AgentToolAttribute"/>; the library deliberately offers no class-per-tool base.
/// </para>
/// <para>
/// That this can be rebuilt in thirty lines is the point worth keeping: the escape hatch is
/// sufficient for a host that genuinely needs a type, and these tests exercise it.
/// </para>
/// </remarks>
public abstract class ProbeTool<TArguments, TResult> : AgentTool
{
    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => AgentToolResultKind.Whole;

    /// <inheritdoc />
    public override JsonElement JsonSchema => AgentToolJson.Schema<TArguments>(this.JsonSerializerOptions);

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => AgentToolJson.Schema<TResult>(this.JsonSerializerOptions);

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        await this.ExecuteAsync(ProbeArgumentReader.Read<TArguments>(arguments, this.JsonSerializerOptions), arguments.RequireAgentToolContext(), cancellationToken)
            .ConfigureAwait(false);

    /// <summary>Runs the tool.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="context">Who is asking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    protected abstract Task<TResult> ExecuteAsync(TArguments arguments, AgentToolContext context, CancellationToken cancellationToken);
}
