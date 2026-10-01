namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// How a tool's result is delivered.
/// </summary>
/// <remarks>
/// The output schema always describes the complete assembled result, whichever kind.
/// </remarks>
public enum AgentToolResultKind
{
    /// <summary>
    /// The result arrives in one piece.
    /// </summary>
    Whole = 0,

    /// <summary>
    /// The result arrives as a sequence of items.
    /// </summary>
    Sequence = 1
}
