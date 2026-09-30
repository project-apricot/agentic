using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// The tripwire: a tool carrying a gate that nothing in this host enforces refuses to compose.
/// </summary>
/// <remarks>
/// <para>
/// This is why there is no setting to turn authorization on. Forgetting the call that enforces it
/// would otherwise open every tool that thought it was gated, and the failure is silent: a tool
/// with <c>[Authorize]</c> on it looks gated in the source and is not.
/// </para>
/// <para>
/// The check is by namespace rather than by type, because this package deliberately does not
/// reference ASP.NET Core - a console host federating MCP servers should not acquire a web
/// framework to say so. The namespace is public and stable, and a host enforcing authorization
/// its own way says so by registering an <see cref="AgentToolEnforcementMarker"/>.
/// </para>
/// </remarks>
/// <param name="marker">Registered by whatever enforces gates, or null where nothing does.</param>
public sealed class EnforcementDeclaredValidator(AgentToolEnforcementMarker? marker = null) : IAgentToolValidator
{
    /// <summary>
    /// The namespace whose interfaces mean "a caller has to satisfy something".
    /// </summary>
    private const string GateNamespace = "Microsoft.AspNetCore.Authorization";

    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (marker is not null)
        {
            return;
        }

        foreach (var metadata in tool.Metadata)
        {
            if (Waives(metadata))
            {
                return;
            }
        }

        foreach (var metadata in tool.Metadata)
        {
            if (Gates(metadata))
            {
                throw new AgentToolDeclarationException(
                    $"The tool '{tool.Name}' carries authorization, but nothing in this host enforces it. " +
                    "Call WithAuthorization(), add an IAgentToolAuthorizationFilter with AddAgentToolAuthorizationFilter(), " +
                    "or register an AgentToolEnforcementMarker beside whatever enforces it instead.");
            }
        }
    }

    /// <summary>
    /// Whether a piece of metadata says a caller has to satisfy something.
    /// </summary>
    /// <param name="metadata">What was said.</param>
    /// <returns>True where it is a gate.</returns>
    private static bool Gates(object metadata) =>
        Implements(metadata, "IAuthorizeData") || Implements(metadata, "IAuthorizationRequirementData");

    /// <summary>
    /// Whether a piece of metadata waives every gate on the tool.
    /// </summary>
    /// <param name="metadata">What was said.</param>
    /// <returns>True where it is an anonymous marker.</returns>
    /// <remarks>
    /// A gate the host waived on purpose is not one nothing is enforcing.
    /// </remarks>
    private static bool Waives(object metadata) => Implements(metadata, "IAllowAnonymous");

    /// <summary>
    /// Whether something implements one of the interfaces this looks for.
    /// </summary>
    /// <param name="metadata">What was said.</param>
    /// <param name="name">The interface's name.</param>
    /// <returns>True where it does.</returns>
    private static bool Implements(object metadata, string name) =>
        metadata.GetType().GetInterfaces().Any(contract =>
            string.Equals(contract.Namespace, GateNamespace, StringComparison.Ordinal) &&
            string.Equals(contract.Name, name, StringComparison.Ordinal));
}
