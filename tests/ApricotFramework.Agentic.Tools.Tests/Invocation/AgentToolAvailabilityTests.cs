using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>What a caller is offered, and that it matches what the gate accepts.</summary>
public class AgentToolAvailabilityTests
{
    private static AgentToolInvoker Invoker(IEnumerable<AgentTool> tools, params IAgentToolFilter[] filters) =>
        new(new AgentToolRegistry([new StaticAgentToolSource([.. tools.Select(tool => new AgentToolDescriptor(tool))])]), filters);

    private static ConfigurableProbe Probe(string name) => new(name);

    [Fact]
    public async Task GetAvailableToolsAsync_UnrestrictedAuthorizer_OffersEverything()
    {
        var tools = await Invoker([Probe("probe_items_one"), Probe("probe_items_two")])
            .GetAvailableToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(2, tools.Count);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_AuthorizerRefusesOne_LeavesItOut()
    {
        var invoker = Invoker([Probe("probe_items_one"), Probe("probe_items_two")], new AllowOnly("probe_items_one"));

        var tools = await invoker.GetAvailableToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal("probe_items_one", Assert.Single(tools).Name);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_AndTheGate_AgreeOnEveryTool()
    {
        // the property the whole arrangement exists for: nothing offered then refuses, and
        // nothing refused was offered
        var all = new[] { Probe("probe_items_one"), Probe("probe_items_two"), Probe("probe_items_three") };
        var invoker = Invoker(all, new AllowOnly("probe_items_two"));

        var offered = await invoker.GetAvailableToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        foreach (var tool in all)
        {
            var accepted = true;

            try
            {
                await invoker.InvokeCompleteAsync(tool.Name, null, Probes.Context(), TestContext.Current.CancellationToken);
            }
            catch (AgentToolAccessDeniedException)
            {
                accepted = false;
            }

            Assert.Equal(offered.Any(entry => entry.Name == tool.Name), accepted);
        }
    }





    [Fact]
    public async Task InvokeAsync_WithABlankToolName_IsReportedAsAbsent()
    {
        await Assert.ThrowsAsync<AgentToolNotFoundException>(
            () => Invoker([Probe("probe_items_one")]).InvokeCompleteAsync(
                string.Empty, null, Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvokeAsync_DeniedTool_CarriesTheFiltersReason()
    {
        // the reason is what a refusal says and what a log records; a tool simply absent, with
        // nobody able to say which filter removed it, is the failure nobody can diagnose
        var exception = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoker([Probe("probe_items_two")], new AllowOnly("probe_items_one"))
                .InvokeCompleteAsync("probe_items_two", null, Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Contains("not on the list", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_SeveralFilters_TheFirstRefusalWins()
    {
        var invoker = Invoker(
            [Probe("probe_items_one")],
            new AllowOnly("probe_items_one"),
            new DenyEverything("the second filter said no"));

        var exception = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => invoker.InvokeCompleteAsync("probe_items_one", null, Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal("the second filter said no", exception.Message);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_NoFiltersAtAll_OffersEverything()
    {
        // a host saying there is nothing to decide. true of a command line tool, false of almost
        // everything else - which is why the ASP.NET Core package registers one for you
        Assert.Equal(2, (await Invoker([Probe("probe_items_one"), Probe("probe_items_two")])
            .GetAvailableToolsAsync(Probes.Context(), TestContext.Current.CancellationToken)).Count);
    }

    /// <summary>A filter that refuses everything, to show ordering.</summary>
    /// <param name="reason">Why.</param>
    private sealed class DenyEverything(string reason) : IAgentToolFilter
    {
        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(AgentToolFilterDecision.Deny(reason));
    }

    /// <summary>A filter permitting only the tools it was named.</summary>
    /// <param name="permitted">The tool names to permit.</param>
    private sealed class AllowOnly(params string[] permitted) : IAgentToolFilter
    {
        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(permitted.Contains(tool.Name, StringComparer.Ordinal)
                ? AgentToolFilterDecision.Allow()
                : AgentToolFilterDecision.Deny($"'{tool.Name}' is not on the list."));
        }
    }
}
