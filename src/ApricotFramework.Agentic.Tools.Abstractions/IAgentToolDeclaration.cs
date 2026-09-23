namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Everything said about a tool that <c>Microsoft.Extensions.AI</c> does not already say.
/// </summary>
/// <remarks>
/// <para>
/// A tool's name, prose, and schemas live on <see cref="Microsoft.Extensions.AI.AIFunctionDeclaration"/>,
/// and are not restated here - two names for one value is how the two come to disagree. What is
/// here is what an agentic caller's policy depends on, and the AI abstractions have no property
/// for.
/// </para>
/// <para>
/// Separate from the function on purpose. A tool read off a foreign server arrives as an
/// <see cref="Microsoft.Extensions.AI.AIFunction"/> that already exists, and wrapping it to
/// attach four booleans would cost it the identity a consumer reaches through
/// <c>GetService()</c>. A declaration sits beside the function instead, in an
/// <see cref="AgentToolDescriptor"/>.
/// </para>
/// </remarks>
public interface IAgentToolDeclaration
{
    /// <summary>
    /// Gets the name a caller invokes this tool by.
    /// </summary>
    /// <remarks>
    /// Shared with every other tool a caller can see, including tools from other applications
    /// entirely, so a name generic enough to shadow one of those is worth avoiding. Any further
    /// convention is the host's, expressed as a validator.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the name a person sees for this tool.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets what this tool is for, written for a model.
    /// </summary>
    /// <remarks>
    /// A prompt, not documentation. It is what a model is shown and what it selects on, so it is
    /// worth saying when to reach for this rather than what the implementation does.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this tool only reads.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this tool can destroy something a caller would not want destroyed.
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets a value indicating whether calling this twice with the same arguments has the same effect as calling it once.
    /// </summary>
    bool IsIdempotent { get; }

    /// <summary>
    /// Gets a value indicating whether this tool reaches something outside the application.
    /// </summary>
    bool IsOpenWorld { get; }

    /// <summary>
    /// Gets how this tool's result arrives.
    /// </summary>
    AgentToolResultKind ResultKind { get; }

    /// <summary>
    /// Gets the host-defined labels this tool carries.
    /// </summary>
    /// <remarks>
    /// The vocabulary is the host's. See <see cref="AgentToolLabelAttribute"/> for why this
    /// library declares none of its own.
    /// </remarks>
    IReadOnlyDictionary<string, object?> Labels { get; }
}
