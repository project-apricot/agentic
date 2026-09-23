using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool base carrying a requirement its derivatives inherit.</summary>
[RequireProbe("inherited")]
public abstract class InheritingProbeBase : ProbeToolBase
{

}
