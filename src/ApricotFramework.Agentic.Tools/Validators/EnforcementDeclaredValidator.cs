using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Rejects a gated tool when nothing in the host enforces gates.
/// </summary>
/// <remarks>Prevents a forgotten enforcement call from silently opening <c>[Authorize]</c> tools. Gates are detected by namespace so this package need not reference ASP.NET Core; hosts enforcing their own way register an <see cref="AgentToolEnforcementMarker"/>.</remarks>
/// <param name="marker">Present when something enforces gates; otherwise null.</param>
public sealed class EnforcementDeclaredValidator(AgentToolEnforcementMarker? marker = null) : IAgentToolValidator
{
    /// <summary>
    /// Namespace of the authorization gate interfaces.
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
    /// Whether metadata is a gate.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <returns>True if it is a gate.</returns>
    private static bool Gates(object metadata) =>
        Implements(metadata, "IAuthorizeData") || Implements(metadata, "IAuthorizationRequirementData");

    /// <summary>
    /// Whether metadata waives every gate on the tool.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <returns>True if it is an anonymous marker.</returns>
    private static bool Waives(object metadata) => Implements(metadata, "IAllowAnonymous");

    /// <summary>
    /// Whether metadata implements a gate-namespace interface by name.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="name">The interface name.</param>
    /// <returns>True if it does.</returns>
    private static bool Implements(object metadata, string name) =>
        metadata.GetType().GetInterfaces().Any(contract =>
            string.Equals(contract.Namespace, GateNamespace, StringComparison.Ordinal) &&
            string.Equals(contract.Name, name, StringComparison.Ordinal));
}
