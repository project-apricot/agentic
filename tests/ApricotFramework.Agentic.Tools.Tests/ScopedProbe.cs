namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// A tool taking a scoped dependency through its constructor, the way an endpoint does.
/// </summary>
/// <param name="dependency">Something the container hands out once per scope.</param>
public sealed class ScopedProbe(ScopedDependency dependency) : ProbeTool<ProbeNoArguments, int>
{
    /// <inheritdoc />
    public override string Name => "probe_scope_read";

    /// <inheritdoc />
    public override string Title => "Read the scope";

    /// <inheritdoc />
    public override string Description => "Reports which scoped dependency this call was given.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<int> ExecuteAsync(ProbeNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) =>
        Task.FromResult(dependency.Ordinal);
}
