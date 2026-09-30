using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Insists every tool declares a description.
/// </summary>
/// <remarks>
/// The one most worth choosing. A description is what a model selects on, so a tool without one cannot be chosen correctly - it is not documentation that can be added later. Still not registered by default, because whether a host wants its own build to fail over it is the host's call.
/// </remarks>
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
