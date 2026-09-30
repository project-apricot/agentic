namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Says that something in this host enforces the gates a tool declares.
/// </summary>
/// <remarks>
/// Registered by <c>WithAuthorization()</c> and by <c>AddAgentToolAuthorizationFilter()</c>, since
/// registering an authorization filter is itself the statement that something enforces. Registrable
/// by hand by a host enforcing authorization entirely outside this pipeline. Without it, <see cref="EnforcementDeclaredValidator"/> refuses to compose a tool that
/// declares a gate.
/// </remarks>
public sealed class AgentToolEnforcementMarker;
