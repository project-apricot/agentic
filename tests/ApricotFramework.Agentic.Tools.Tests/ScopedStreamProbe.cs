using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// A sequence tool holding a scoped dependency across every item it yields.
/// </summary>
/// <param name="dependency">Something the container hands out once per scope.</param>
/// <remarks>
/// A tool holding a unit of work open across a long sequence is the ordinary case, and a scope
/// disposed at the first item would break it. This is what checks that it is not.
/// </remarks>
public sealed class ScopedStreamProbe(ScopedDependency dependency) : ProbeStreamTool<ProbeNoArguments, int>
{
    /// <inheritdoc />
    public override string Name => "probe_scope_stream";

    /// <inheritdoc />
    public override string Title => "Stream from the scope";

    /// <inheritdoc />
    public override string Description => "Reports how many scoped dependencies were disposed before each item.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override async IAsyncEnumerable<int> ExecuteAsync(
        ProbeNoArguments arguments,
        AgentToolContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 0; index < 3; index++)
        {
            await Task.Yield();

            // reading the dependency at each step, so a disposed scope would be visible here
            yield return dependency.Ordinal + ScopedDependency.Disposed;
        }
    }
}
