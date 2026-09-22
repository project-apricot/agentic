using ApricotFramework.Agentic.Tools;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The base every support desk tool that returns one result derives from.
/// </summary>
/// <typeparam name="TArguments">What the tool is called with.</typeparam>
/// <typeparam name="TResult">What it reports back.</typeparam>
/// <remarks>
/// What a family of tools has in common, said once. Labels are inherited by every tool below this,
/// and an authorization attribute here would be too - a derived tool restating a label overrides
/// it, and an attribute it adds is required on top rather than instead.
/// </remarks>
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
public abstract class SupportDeskTool<TArguments, TResult> : AgentTool<TArguments, TResult>;
