using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Requires every tool to declare a title.
/// </summary>
/// <remarks>Not registered by default.</remarks>
public sealed class TitleDeclaredValidator : IAgentToolValidator
{
    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (string.IsNullOrWhiteSpace(tool.Declaration.Title))
        {
            throw new AgentToolDeclarationException($"The tool '{tool.Name}' declares no title.");
        }
    }
}
