using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>A tool that declares a requirement and then waives it.</summary>
[RequireProbe("not-granted")]
[AllowAnonymous]
public sealed class AnonymousProbe : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => "probe_items_anonymous";

    /// <inheritdoc />
    public override string Title => "Probe";

    /// <inheritdoc />
    public override string Description => "Waives what it declares.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
}
