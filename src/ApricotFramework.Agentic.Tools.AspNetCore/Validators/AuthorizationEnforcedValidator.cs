using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Validators;

/// <summary>
/// Refuses to serve a tool that carries authorization nothing is enforcing.
/// </summary>
/// <remarks>
/// <para>
/// The tripwire and the reason there is no setting to turn authorization on. A tool that declares
/// authorization while nothing enforces it is a configuration error, not a permission - and
/// without this, forgetting <c>AddAgentToolAuthorization</c> would quietly open every tool that
/// thought it was gated. The MCP SDK makes the same call for the same reason.
/// </para>
/// <para>
/// It says nothing about a tool that carries no authorization. A host with genuinely open tools is
/// a legitimate configuration; a host that wrote <c>[Authorize]</c> and did not wire it up is not.
/// </para>
/// <para>
/// Registered by <c>AddAgentTools</c>, and fires while the registry composes - so the failure
/// lands at startup rather than on whichever request arrives first.
/// </para>
/// </remarks>
/// <param name="marker">Present when something in this host enforces authorization.</param>
public sealed class AuthorizationEnforcedValidator(AgentToolAuthorizationMarker? marker = null) : IAgentToolValidator
{
    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (marker is not null || !AgentToolAuthorizationMetadata.For(tool).IsDeclared)
        {
            return;
        }

        throw new AgentToolDeclarationException(
            $"The tool '{tool.Name}' carries authorization, but nothing in this host enforces it. " +
            $"Call AddAgentToolAuthorization(), or register an {nameof(AgentToolAuthorizationMarker)} beside whatever enforces it instead.");
    }
}
