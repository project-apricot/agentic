namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// The arguments of a tool that takes none.
/// </summary>
/// <remarks>
/// A type rather than a second-base class without the argument parameter. It keeps one generic
/// shape for every tool, and it gives a tool that later grows an argument somewhere to put it
/// without changing what it derives from.
/// </remarks>
public sealed record AgentToolNoArguments;
