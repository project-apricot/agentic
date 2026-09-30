using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Validators;

/// <summary>
/// Insists every tool is named <c>support_&lt;resource&gt;_&lt;verb&gt;</c>.
/// </summary>
/// <remarks>
/// A convention the library has no opinion about, so it lives here. Resource before verb so that
/// everything acting on the same thing sorts together, which is what helps a model narrow from a
/// long list. The prefix keeps these names out of the way of another application's.
/// </remarks>
public sealed class SupportDeskNamingValidator : IAgentToolValidator
{
    /// <summary>The prefix every name sits under.</summary>
    private const string Prefix = "support";

    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (tool.Name.Any(character => !char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character != '_'))
        {
            throw new AgentToolDeclarationException($"The tool name '{tool.Name}' is not lowercase snake case.");
        }

        var segments = tool.Name.Split('_');

        if (segments.Length < 3 || segments.Any(string.IsNullOrEmpty))
        {
            throw new AgentToolDeclarationException($"The tool name '{tool.Name}' is not <resource>_<verb> under a prefix.");
        }

        if (!string.Equals(segments[0], Prefix, StringComparison.Ordinal))
        {
            throw new AgentToolDeclarationException($"The tool name '{tool.Name}' is not under the '{Prefix}' prefix this application declares tools under.");
        }
    }
}
