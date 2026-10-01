using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Requires every tool to declare a description.
/// </summary>
/// <remarks>Models select tools by description. Not registered by default.</remarks>
public sealed class DescriptionDeclaredValidator : IAgentToolValidator
{
    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (string.IsNullOrWhiteSpace(tool.Declaration.Description))
        {
            throw new AgentToolDeclarationException($"The tool '{tool.Name}' declares no description.");
        }
    }
}
