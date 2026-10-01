using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Rejects a tool declared both read-only and destructive.
/// </summary>
/// <remarks>Not registered by default.</remarks>
public sealed class ConsistentBehaviourValidator : IAgentToolValidator
{
    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (tool.Declaration is { IsReadOnly: true, IsDestructive: true })
        {
            throw new AgentToolDeclarationException($"The tool '{tool.Name}' declares itself both read only and destructive.");
        }
    }
}
