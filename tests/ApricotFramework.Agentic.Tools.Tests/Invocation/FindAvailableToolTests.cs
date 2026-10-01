using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>Deciding the one tool a surface is about to call, and nothing else.</summary>
public class FindAvailableToolTests
{
    [Fact]
    public async Task AToolOfferedAndPermitted_IsFound_AndOnlyThatToolIsPutToTheLayers()
    {
        var authorization = new Counting(allow: true);

        var invoker = new AgentToolInvoker(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe(), new SequenceProbe())]), null, [authorization]);

        var found = await invoker.FindAvailableToolAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal("probe_items_get", found?.Name);
        Assert.Equal(1, authorization.Calls);
    }

    [Fact]
    public async Task AToolRefused_IsNotFound()
    {
        var invoker = new AgentToolInvoker(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe())]), null, [new Counting(allow: false)]);

        Assert.Null(await invoker.FindAvailableToolAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheExecutor_HandsBackAFunctionThatRunsThroughIt()
    {
        var services = new ServiceCollection();

        services.AddAgentToolsCore();
        services.AddAgentToolSource(_ => StaticAgentToolSource.For(new SingleProbe()));

        await using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        var executor = host.GetRequiredService<IAgentToolExecutor>();

        var function = await executor.GetAvailableFunctionAsync("probe_items_get", TestContext.Current.CancellationToken);

        Assert.NotNull(function);
        Assert.Null(await executor.GetAvailableFunctionAsync("probe_items_missing", TestContext.Current.CancellationToken));
    }

    private sealed class Counting(bool allow) : IAgentToolAuthorizationFilter
    {
        public int Calls { get; private set; }

        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return ValueTask.FromResult(allow ? AgentToolAuthorizationDecision.Allow() : AgentToolAuthorizationDecision.Deny("no"));
        }
    }
}
