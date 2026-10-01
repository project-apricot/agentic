namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Excludes a tool from discovery by scanning.
/// </summary>
/// <remarks>
/// Honored by <c>AgentToolDiscovery</c>; explicit registration still works.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class AgentToolIgnoreAttribute : Attribute;
