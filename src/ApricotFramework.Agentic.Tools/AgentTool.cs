using ApricotFramework.Agentic.Tools.Adapters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Options;
using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// One operation an AI agent can perform.
/// </summary>
/// <remarks>
/// <para>
/// A tool is declared beside its implementation and nowhere else: the name, the prose a model
/// selects on, the schemas it fills in and reads back, and the behavior a caller's policy depends
/// on.
/// </para>
/// <para>
/// Nothing here says who may reach a tool either. What a caller must hold is declared in whatever
/// way the host already declares it - an authorization attribute, most usefully - collected onto
/// the <see cref="AgentToolDescriptor"/> at registration and decided by an
/// <see cref="IAgentToolFilter"/>. A library that understood permissions would have to understand
/// one application's model of them.
/// </para>
/// <para>
/// Not every operation should become one of these. An oversized tool surface makes a model worse
/// at choosing between what is on it, so a surface is worth growing by what an agent is
/// demonstrably useful for rather than by what happens to be implemented.
/// </para>
/// </remarks>
public abstract class AgentTool
{
    /// <summary>
    /// Gets the name a caller invokes this tool by.
    /// </summary>
    /// <remarks>
    /// Shared with every other tool a caller can see, including tools from other applications
    /// entirely, so a name generic enough to shadow one of those is worth avoiding. Any further
    /// convention is the host's, expressed as an <see cref="IAgentToolValidator"/>.
    /// </remarks>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the name a person sees for this tool.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Gets what this tool is for, written for a model.
    /// </summary>
    /// <remarks>
    /// A prompt, not documentation. It is what a model is shown and what it selects on, so it is
    /// worth saying when to reach for this rather than what the implementation does. The code
    /// comments around it are for whoever maintains the tool; the two are not duplicates.
    /// </remarks>
    public abstract string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this tool only reads.
    /// </summary>
    public abstract bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this tool can destroy something a caller would not want destroyed.
    /// </summary>
    public abstract bool IsDestructive { get; }

    /// <summary>
    /// Gets a value indicating whether calling this twice with the same arguments has the same effect as calling it once.
    /// </summary>
    /// <remarks>
    /// Follows <see cref="IsReadOnly"/> unless overridden, which is right for a read and has to be
    /// stated by any writing that is not idempotent.
    /// </remarks>
    public virtual bool IsIdempotent => this.IsReadOnly;

    /// <summary>
    /// Gets a value indicating whether this tool reaches something outside the application.
    /// </summary>
    public virtual bool IsOpenWorld => false;

    /// <summary>
    /// Gets how this tool's result arrives.
    /// </summary>
    public abstract AgentToolResultKind ResultKind { get; }

    /// <summary>
    /// Gets the schema of the arguments this tool takes.
    /// </summary>
    public abstract JsonElement InputSchema { get; }

    /// <summary>
    /// Gets the schema of the complete result this tool returns, or null where it describes none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole result even when <see cref="ResultKind"/> is
    /// <see cref="AgentToolResultKind.Sequence"/> - an array rather than one of its items. A
    /// consumer then never has to reconstruct the outer shape, and one of them would forget.
    /// </para>
    /// <para>
    /// Nullable because not every tool can describe its result. One adapted from a function
    /// returning nothing has none to give, and one adapted from a foreign tool has only whatever
    /// that tool chose to publish.
    /// </para>
    /// </remarks>
    public abstract JsonElement? OutputSchema { get; }

    /// <summary>
    /// Gets the host-defined labels this tool carries.
    /// </summary>
    /// <remarks>
    /// Read from <see cref="AgentToolLabelAttribute"/> unless a tool says otherwise, which is what
    /// lets a tool built at run time carry them too. See that attribute for why the vocabulary is
    /// the host's rather than this library's.
    /// </remarks>
    public virtual IReadOnlyDictionary<string, object?> Labels => AgentToolLabels.ForType(this.GetType());

    /// <summary>
    /// Gets how this tool's arguments and result are read and written.
    /// </summary>
    /// <remarks>
    /// The same options generate this tool's schemas, so the two cannot disagree about property
    /// names. Overridden by a tool or by a base class shared across a host's tools, where the
    /// default naming or converters are not what the host wants.
    /// </remarks>
    public virtual JsonSerializerOptions SerializerOptions => AgentToolJson.DefaultSerializerOptions;

    /// <summary>
    /// Runs the tool.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON, or null or empty where there are none.</param>
    /// <param name="context">Who is asking, and whatever else the host carries.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, as one item for a whole result or as a sequence of them.</returns>
    /// <remarks>
    /// Reached through <see cref="AgentToolInvoker"/>, which is what enforces authorization.
    /// Nothing here decides what a caller may see.
    /// </remarks>
    public abstract IAsyncEnumerable<object?> InvokeAsync(string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a tool backed by an <see cref="Microsoft.Extensions.AI.AIFunction"/>.
    /// </summary>
    /// <param name="function">The function behind the tool.</param>
    /// <param name="options">What the function cannot say for itself - who may see it, and whether it changes anything.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <remarks>
    /// For a tool that is not a class somebody wrote: a delegate, something built from
    /// configuration, or a tool read off a server the host speaks to - an MCP client's tools are
    /// <see cref="Microsoft.Extensions.AI.AIFunction"/> instances, so they arrive through here too.
    /// </remarks>
    public static FunctionAgentTool Create(AIFunction function, AgentToolCreateOptions options)
    {
        return new FunctionAgentTool(function, options);
    }
}
