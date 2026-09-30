using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool that declares a requirement and then waives it.</summary>
[RequireProbe("not-granted")]
[AllowAnonymous]
public sealed class AnonymousProbe : ProbeToolBase
{
    /// <inheritdoc />
    public override string Name => "probe_items_anonymous";

}
