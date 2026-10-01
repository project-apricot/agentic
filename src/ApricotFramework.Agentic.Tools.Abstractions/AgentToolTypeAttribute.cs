namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Marks a class whose methods carrying <see cref="AgentToolAttribute"/> are tools.
/// </summary>
/// <remarks>
/// The class is resolved from the container once per invocation, so it can take scoped
/// dependencies through its constructor.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AgentToolTypeAttribute : Attribute;
