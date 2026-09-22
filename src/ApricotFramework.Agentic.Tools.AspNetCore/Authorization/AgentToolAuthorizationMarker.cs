namespace ApricotFramework.Agentic.Tools.AspNetCore.Authorization;

/// <summary>
/// Says that something in this host enforces the authorization a tool declares.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>AddAgentToolAuthorization</c>, and looked for by
/// <see cref="Validators.AuthorizationEnforcedValidator"/>. The pair is what lets a host forget
/// the call and be told, rather than forget it and be open.
/// </para>
/// <para>
/// Public because a host may enforce authorization its own way - a filter reading a table, a
/// service the framework has never heard of. Registering this alongside it says so and stops the
/// tripwire from complaining about a job somebody else is doing.
/// </para>
/// </remarks>
public sealed class AgentToolAuthorizationMarker;
