namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// How a tool's result arrives.
/// </summary>
/// <remarks>
/// About delivery, not shape. A tool's output schema always describes the complete assembled
/// result either way, so a consumer that cannot stream can read the schema and buffer without
/// having to reconstruct the outer shape itself.
/// </remarks>
public enum AgentToolResultKind
{
    /// <summary>
    /// The result arrives whole, in one piece.
    /// </summary>
    Whole = 0,

    /// <summary>
    /// The result arrives as a sequence of items that together form the result.
    /// </summary>
    Sequence = 1
}
