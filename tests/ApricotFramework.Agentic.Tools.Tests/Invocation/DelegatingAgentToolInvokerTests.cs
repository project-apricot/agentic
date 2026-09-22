using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>Wrapping an invoker.</summary>
public class DelegatingAgentToolInvokerTests
{
    private static AgentToolInvoker Inner() =>
        new(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe(), new SequenceProbe())]),
            null);

    private const string Arguments = """{"id":1}""";

    private static AgentToolContext Caller() => new();

    [Fact]
    public async Task Delegating_PassesEverythingThroughByDefault()
    {
        IAgentToolInvoker wrapped = new PassThrough(Inner());

        Assert.Equal(2, (await wrapped.GetAvailableToolsAsync(Caller(), TestContext.Current.CancellationToken)).Count);

        Assert.Contains("probe", await wrapped.InvokeCompleteAsync("probe_items_get", Arguments, Caller(), TestContext.Current.CancellationToken), StringComparison.Ordinal);

        var chunks = new List<string>();

        await foreach (var chunk in wrapped.InvokeAsync("probe_items_list", Arguments, Caller(), TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(3, chunks.Count);
    }

    [Fact]
    public async Task Delegating_SeesEveryCall()
    {
        var counting = new Counting(Inner());

        await counting.GetAvailableToolsAsync(Caller(), TestContext.Current.CancellationToken);
        await counting.InvokeCompleteAsync("probe_items_get", Arguments, Caller(), TestContext.Current.CancellationToken);

        await foreach (var _ in counting.InvokeAsync("probe_items_list", Arguments, Caller(), TestContext.Current.CancellationToken))
        {
        }

        Assert.Equal(1, counting.Listings);
        Assert.Equal(1, counting.Completions);
        Assert.Equal(1, counting.Streams);
    }

    [Fact]
    public async Task Delegating_Chained_AppliesOutermostFirst()
    {
        var order = new List<string>();

        IAgentToolInvoker chain = new Recording(new Recording(Inner(), "inner", order), "outer", order);

        await chain.InvokeCompleteAsync("probe_items_get", Arguments, Caller(), TestContext.Current.CancellationToken);

        Assert.Equal(["outer", "inner"], order);
    }

    [Fact]
    public async Task Delegating_OverridingOneMember_LeavesTheOthersAlone()
    {
        IAgentToolInvoker wrapped = new HideEverything(Inner());

        Assert.Empty(await wrapped.GetAvailableToolsAsync(Caller(), TestContext.Current.CancellationToken));

        // the listing is hidden, the call is not - a wrapper changes only what it overrides, which
        // is also the trap: a rate limit on one of the two is a rate limit on neither
        Assert.Contains("probe", await wrapped.InvokeCompleteAsync("probe_items_get", Arguments, Caller(), TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    /// <summary>A wrapper that changes nothing.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    private sealed class PassThrough(IAgentToolInvoker inner) : DelegatingAgentToolInvoker(inner);

    /// <summary>A wrapper that counts what it saw.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    private sealed class Counting(IAgentToolInvoker inner) : DelegatingAgentToolInvoker(inner)
    {
        public int Listings { get; private set; }

        public int Completions { get; private set; }

        public int Streams { get; private set; }

        public override ValueTask<IReadOnlyList<AgentTool>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Listings++;

            return base.GetAvailableToolsAsync(context, cancellationToken);
        }

        public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Completions++;

            return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
        }

        public override IAsyncEnumerable<string> InvokeAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Streams++;

            return base.InvokeAsync(name, argumentsJson, context, cancellationToken);
        }
    }

    /// <summary>A wrapper that records the order it was reached in.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    /// <param name="label">What to record.</param>
    /// <param name="order">Where to record it.</param>
    private sealed class Recording(IAgentToolInvoker inner, string label, List<string> order) : DelegatingAgentToolInvoker(inner)
    {
        public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            order.Add(label);

            return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
        }
    }

    /// <summary>A wrapper that offers nothing but still lets calls through.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    private sealed class HideEverything(IAgentToolInvoker inner) : DelegatingAgentToolInvoker(inner)
    {
        public override ValueTask<IReadOnlyList<AgentTool>> GetAvailableToolsAsync(AgentToolContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<AgentTool>>([]);
        }
    }
}
