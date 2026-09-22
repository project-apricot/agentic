namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Checks a tool's registration while the registry is being built.
/// </summary>
/// <remarks>
/// <para>
/// The extension point for rules this library cannot hold an opinion about. A naming convention
/// shared across an organization, closed vocabulary of labels, a requirement that every tool
/// declares a description - each is right somewhere and wrong somewhere else, and each belongs in
/// one of these rather than in a setting.
/// </para>
/// <para>
/// Nothing is registered by default beyond what the ASP.NET Core package needs to keep its own
/// promise. The validators this library ships are there to be chosen; see
/// <c>ApricotFramework.Agentic.Tools.Validators</c>.
/// </para>
/// <para>
/// Given the descriptor rather than the tool, so a check can read what was said at registration
/// as well as what the tool declares about itself.
/// </para>
/// </remarks>
public interface IAgentToolValidator
{
    /// <summary>
    /// Checks one registration.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <exception cref="Exceptions.AgentToolDeclarationException">Thrown when the registration is not acceptable.</exception>
    void Validate(AgentToolDescriptor tool);
}
