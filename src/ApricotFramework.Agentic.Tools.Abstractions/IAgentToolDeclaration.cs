namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Tool metadata not covered by <c>Microsoft.Extensions.AI</c>.
/// </summary>
/// <remarks>
/// Name, description and schemas live on <see cref="Microsoft.Extensions.AI.AIFunctionDeclaration"/>.
/// Kept separate from the function so a foreign <see cref="Microsoft.Extensions.AI.AIFunction"/>
/// need not be wrapped; the two are paired in an <see cref="AgentToolDescriptor"/>.
/// </remarks>
public interface IAgentToolDeclaration
{
    /// <summary>
    /// Gets the name a caller invokes this tool by.
    /// </summary>
    /// <remarks>
    /// Shares a namespace with tools from other applications, so avoid overly generic names.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the human-readable name.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the description shown to the model.
    /// </summary>
    /// <remarks>
    /// A prompt, not documentation: say when to use the tool rather than how it works.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this tool only reads.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this tool may perform destructive changes.
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets a value indicating whether repeated calls with the same arguments have no further effect.
    /// </summary>
    bool IsIdempotent { get; }

    /// <summary>
    /// Gets a value indicating whether this tool reaches outside the application.
    /// </summary>
    bool IsOpenWorld { get; }

    /// <summary>
    /// Gets how this tool's result arrives.
    /// </summary>
    AgentToolResultKind ResultKind { get; }

    /// <summary>
    /// Gets the host-defined labels.
    /// </summary>
    IReadOnlyDictionary<string, object?> Labels { get; }
}
