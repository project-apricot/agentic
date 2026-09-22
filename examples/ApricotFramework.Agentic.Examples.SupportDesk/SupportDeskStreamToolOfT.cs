using ApricotFramework.Agentic.Tools;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The base every support desk tool that returns a sequence derives from.
/// </summary>
/// <typeparam name="TArguments">What the tool is called with.</typeparam>
/// <typeparam name="TItem">One item of the result.</typeparam>
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
public abstract class SupportDeskStreamTool<TArguments, TItem> : AgentStreamTool<TArguments, TItem>;
