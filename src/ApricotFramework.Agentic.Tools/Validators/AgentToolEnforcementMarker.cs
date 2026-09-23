namespace ApricotFramework.Agentic.Tools.Validators;

/// <summary>
/// Says that something in this host enforces the gates a tool declares.
/// </summary>
/// <remarks>
/// Registered by <c>AddAgentToolAuthorization()</c>, and registrable by hand by a host enforcing
/// authorization its own way - a filter reading a table, a service this library has never heard
/// of. Without it, <see cref="EnforcementDeclaredValidator"/> refuses to compose a tool that
/// declares a gate.
/// </remarks>
public sealed class AgentToolEnforcementMarker;
