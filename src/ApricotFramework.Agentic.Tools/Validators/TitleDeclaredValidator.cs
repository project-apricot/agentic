using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Insists every tool declares a title.
/// </summary>
/// <remarks>
/// For a host with a surface that shows one. Not registered by default: a library that refused to serve over a missing label would be one a host works around.
/// </remarks>
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
