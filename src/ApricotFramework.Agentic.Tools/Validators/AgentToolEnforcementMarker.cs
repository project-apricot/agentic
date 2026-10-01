namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Declares that this host enforces the gates tools declare.
/// </summary>
/// <remarks>Registered by <c>WithAuthorization()</c> and <c>AddAgentToolAuthorizationFilter()</c>; register it by hand if authorization is enforced outside this pipeline. Without it, <see cref="EnforcementDeclaredValidator"/> rejects gated tools.</remarks>
public sealed class AgentToolEnforcementMarker;
