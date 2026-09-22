using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Serialization;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>Invocation, and the gate every call passes through.</summary>
public class AgentToolInvokerTests
{
    private static AgentToolInvoker Invoker(params IAgentToolFilter[] filters) =>
        new(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe(), new SequenceProbe(), new FailingSequenceProbe())]), filters);

    private static AgentToolContext Caller() => new();

    [Fact]
    public async Task InvokeCompleteAsync_WholeResultTool_ReturnsTheObject()
    {
        var json = await Invoker().InvokeCompleteAsync("probe_items_get", """{"id":7}""", Caller(), TestContext.Current.CancellationToken);

        Assert.Equal(7, JsonSerializer.Deserialize<ProbeResult>(json, AgentToolJson.DefaultSerializerOptions)!.Id);
    }

    [Fact]
    public async Task InvokeCompleteAsync_SequenceTool_ReturnsAnArrayMatchingTheOutputSchema()
    {
        var json = await Invoker().InvokeCompleteAsync("probe_items_list", null, Caller(), TestContext.Current.CancellationToken);

        Assert.Equal(3, JsonSerializer.Deserialize<List<ProbeResult>>(json, AgentToolJson.DefaultSerializerOptions)!.Count);
    }

    [Fact]
    public async Task InvokeAsync_SequenceTool_ReportsOneChunkPerItem()
    {
        var chunks = new List<string>();

        await foreach (var chunk in Invoker().InvokeAsync("probe_items_list", null, Caller(), TestContext.Current.CancellationToken))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(3, chunks.Count);
    }

    [Fact]
    public async Task InvokeCompleteAsync_SequenceFailsPartWayThrough_DiscardsWhatArrived()
    {
        // a truncated result a model cannot recognise as truncated is worse than an error
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Invoker().InvokeCompleteAsync("probe_items_fail", null, Caller(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvokeCompleteAsync_UnknownTool_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<AgentToolNotFoundException>(
            () => Invoker().InvokeCompleteAsync("probe_items_obliterate", null, Caller(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvokeCompleteAsync_AuthorizerRefuses_ThrowsAndDoesNotRun()
    {
        var filter = new RefuseEverything();

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoker(filter).InvokeCompleteAsync("probe_items_get", """{"id":7}""", Caller(), TestContext.Current.CancellationToken));

        Assert.Equal(1, filter.Calls);
    }

    [Fact]
    public async Task InvokeAsync_AuthorizerRefuses_YieldsNothing()
    {
        var invoker = Invoker(new RefuseEverything());

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(async () =>
        {
            await foreach (var _ in invoker.InvokeAsync("probe_items_list", null, Caller(), TestContext.Current.CancellationToken))
            {
                Assert.Fail("a refused call reported an item");
            }
        });
    }

    [Fact]
    public async Task InvokeAsync_Context_ReachesTheAuthorizerAndTheTool()
    {
        var filter = new CaptureContext();

        var context = new AgentToolContext();
        context.Items["tenant"] = "acme";

        await Invoker(filter).InvokeCompleteAsync("probe_items_get", """{"id":1}""", context, TestContext.Current.CancellationToken);

        Assert.Equal("acme", filter.Seen!.Items["tenant"]);
    }

    [Fact]
    public async Task InvokeAsync_DerivedContext_ArrivesUncast()
    {
        // a host with state worth naming derives, and its own tools read a property
        var filter = new CaptureContext();

        await Invoker(filter).InvokeCompleteAsync(
            "probe_items_get", """{"id":1}""", new TenantContext { Tenant = "acme" }, TestContext.Current.CancellationToken);

        Assert.Equal("acme", Assert.IsType<TenantContext>(filter.Seen).Tenant);
    }

    /// <summary>A context a host might derive.</summary>
    private sealed class TenantContext : AgentToolContext
    {
        /// <summary>Gets the tenant.</summary>
        public required string Tenant { get; init; }
    }

    /// <summary>A filter that records what it was given.</summary>
    private sealed class CaptureContext : IAgentToolFilter
    {
        /// <summary>Gets the context it last saw.</summary>
        public AgentToolContext? Seen { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Seen = context;

            return ValueTask.FromResult(AgentToolFilterDecision.Allow());
        }
    }

    /// <summary>A filter that refuses, and counts.</summary>
    private sealed class RefuseEverything : IAgentToolFilter
    {
        /// <summary>Gets how many times it was consulted.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return ValueTask.FromResult(AgentToolFilterDecision.Deny("no"));
        }
    }
}
