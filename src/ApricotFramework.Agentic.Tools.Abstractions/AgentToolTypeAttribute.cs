namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Marks a class whose methods are tools.
/// </summary>
/// <remarks>
/// <para>
/// The other way to write a tool: a class of ordinary methods, each carrying
/// <see cref="AgentToolAttribute"/>, rather than a class per tool. Worth reaching for where a
/// tool takes one identifier and returns one record, and the argument type would be ceremony.
/// </para>
/// <para>
/// The class is resolved from the container once per invocation, so it takes its dependencies
/// through its constructor exactly as a class-per-tool does - including scoped ones.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AgentToolTypeAttribute : Attribute;
