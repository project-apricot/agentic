using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Refuses a tool that describes itself in two contradictory ways.
/// </summary>
/// <remarks>
/// Not a judgment about what a declaration ought to carry, unlike the other two: a tool cannot
/// both only read and destroy something, and whichever of the two is wrong, one of them is. Worth
/// registering for that reason, and still not registered by default, because nothing here decides
/// what a host checks.
/// </remarks>
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
