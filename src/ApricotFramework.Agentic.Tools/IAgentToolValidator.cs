namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Validates a tool's registration while the registry is being built.
/// </summary>
/// <remarks>
/// For host-specific rules (naming, labels, required descriptions). None are registered by default
/// beyond what the ASP.NET Core package needs; shipped ones are in <c>ApricotFramework.Agentic.Tools.Validators</c>.
/// </remarks>
public interface IAgentToolValidator
{
    /// <summary>
    /// Validates one registration.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <exception cref="Exceptions.AgentToolDeclarationException">The registration is not acceptable.</exception>
    void Validate(AgentToolDescriptor tool);
}
