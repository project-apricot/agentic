namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Keeps a tool out of whatever discovers by scanning.
/// </summary>
/// <remarks>
/// <para>
/// For the tool that is a tool in every respect except that this host should not offer it: one
/// kept for a test, one half-written, one belonging to a surface this deployment does not serve.
/// Registering it by hand still works - this governs discovery, not registration.
/// </para>
/// <para>
/// Honored by <c>AgentToolDiscovery</c>, and therefore by the registration helpers built
/// on it. A host discovering tools some other way is free to ignore it, though a host is unlikely
/// to want to.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class AgentToolIgnoreAttribute : Attribute;
