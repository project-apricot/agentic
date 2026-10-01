using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Maps a host's own exceptions to <see cref="AgentToolFailedException"/> (or a refusal) for every surface.
/// </summary>
/// <remarks>
/// Translators are asked in registration order; the first non-null answer wins, keeping the
/// original as its inner exception. Library exceptions and cancellations are never offered.
/// </remarks>
public interface IAgentToolExceptionTranslator
{
    /// <summary>
    /// Translates an exception a tool raised.
    /// </summary>
    /// <param name="exception">What the tool raised.</param>
    /// <param name="tool">The tool that raised it.</param>
    /// <returns>The translation, or null if not recognized.</returns>
    AgentToolException? Translate(Exception exception, AgentToolDescriptor tool);
}
