using ApricotFramework.Agentic.Tools.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// A tool that returns its result whole.
/// </summary>
/// <typeparam name="TArguments">What the tool is called with. Use <see cref="AgentToolNoArguments"/> where it takes nothing.</typeparam>
/// <typeparam name="TResult">What the tool reports back.</typeparam>
/// <remarks>
/// The schemas come from the two type parameters, so a tool declares its shape by declaring its
/// types. Changing what a tool returns is then a change to one record, and the advertised schema
/// follows, with nobody having to remember to update it.
/// </remarks>
public abstract class AgentTool<TArguments, TResult> : AgentTool
{
    /// <inheritdoc />
    public sealed override AgentToolResultKind ResultKind => AgentToolResultKind.Whole;

    /// <inheritdoc />
    public sealed override JsonElement InputSchema => AgentToolJson.Schema<TArguments>(this.SerializerOptions);

    /// <inheritdoc />
    public sealed override JsonElement? OutputSchema => AgentToolJson.Schema<TResult>(this.SerializerOptions);

    /// <inheritdoc />
    public sealed override async IAsyncEnumerable<object?> InvokeAsync(string? argumentsJson, AgentToolContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var arguments = AgentToolArguments.Read<TArguments>(argumentsJson, this.SerializerOptions);

        yield return await this.ExecuteAsync(arguments, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the tool against arguments already read.
    /// </summary>
    /// <param name="arguments">The arguments the caller supplied.</param>
    /// <param name="context">The invocation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    protected abstract Task<TResult> ExecuteAsync(TArguments arguments, AgentToolContext context, CancellationToken cancellationToken);
}
