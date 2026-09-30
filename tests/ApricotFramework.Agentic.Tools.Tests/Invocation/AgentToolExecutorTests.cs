using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>
/// The layer that opens a scope, decides who is asking, and runs the call inside both.
/// </summary>
public class AgentToolExecutorTests
{
    private static ServiceProvider Host(Action<IServiceCollection>? configure = null)
    {
        ScopedDependency.Reset();

        var services = new ServiceCollection();

        services.AddScoped<ScopedDependency>();
        services.AddAgentToolsCore();
        services.AddAgentTool<ScopedProbe>();
        services.AddAgentTool<ScopedStreamProbe>();
        services.AddAgentToolType<MethodProbes>();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task InvokeCompleteAsync_TwoCalls_GetTwoScopedDependencies()
    {
        // the reason any of this changed. under the previous design one instance was built from
        // the root container at start-up and every caller shared whatever it had captured
        var executor = Host().GetRequiredService<IAgentToolExecutor>();

        var first = await executor.InvokeCompleteAsync("probe_scope_read", null, TestContext.Current.CancellationToken);
        var second = await executor.InvokeCompleteAsync("probe_scope_read", null, TestContext.Current.CancellationToken);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task InvokeCompleteAsync_MethodTool_AlsoGetsAScopeOfItsOwn()
    {
        var executor = Host().GetRequiredService<IAgentToolExecutor>();

        var first = await executor.InvokeCompleteAsync("probe_methods_scope", null, TestContext.Current.CancellationToken);
        var second = await executor.InvokeCompleteAsync("probe_methods_scope", null, TestContext.Current.CancellationToken);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task InvokeCompleteAsync_WhenTheCallEnds_TheScopeIsDisposed()
    {
        var executor = Host().GetRequiredService<IAgentToolExecutor>();

        await executor.InvokeCompleteAsync("probe_scope_read", null, TestContext.Current.CancellationToken);

        Assert.Equal(1, ScopedDependency.Disposed);
    }

    [Fact]
    public async Task InvokeAsync_SequenceTool_KeepsTheScopeUntilTheLastItem()
    {
        // a tool holding a unit of work open across a long sequence is the ordinary case, and a
        // scope disposed at the first item would break it in a way nothing else would catch
        var executor = Host().GetRequiredService<IAgentToolExecutor>();

        var items = new List<string>();

        await foreach (var item in executor.InvokeAsync("probe_scope_stream", null, TestContext.Current.CancellationToken))
        {
            Assert.Equal(0, ScopedDependency.Disposed);

            items.Add(item);
        }

        Assert.Equal(3, items.Count);
        Assert.Equal(1, ScopedDependency.Disposed);
    }

    [Fact]
    public async Task InvokeAsync_CallerStopsReadingEarly_TheScopeIsStillDisposed()
    {
        var executor = Host().GetRequiredService<IAgentToolExecutor>();

        await foreach (var _ in executor.InvokeAsync("probe_scope_stream", null, TestContext.Current.CancellationToken))
        {
            break;
        }

        Assert.Equal(1, ScopedDependency.Disposed);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_ListsWithoutBuildingATool()
    {
        // a listing reads a declaration taken once, so offering twenty tools does not mean
        // constructing twenty objects on every request
        var host = Host();

        var executor = host.GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        var before = ScopedDependency.Disposed;

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(before, ScopedDependency.Disposed);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_OffersBothWaysOfWritingATool()
    {
        var names = (await Host().GetRequiredService<IAgentToolExecutor>()
            .GetAvailableToolsAsync(TestContext.Current.CancellationToken))
            .Select(tool => tool.Name)
            .OrderBy(name => name, StringComparer.Ordinal);

        // every one of these was named deliberately - the attribute takes the name as a
        // constructor argument, so leaving it out does not compile
        Assert.Equal(
            ["probe_methods_add", "probe_methods_caller", "probe_methods_read", "probe_methods_scope", "probe_scope_read", "probe_scope_stream"],
            names);
    }

    [Fact]
    public async Task GetAvailableFunctionsAsync_ReturnsFunctionsAChatClientCanCall()
    {
        // the unification claim: a tool goes into ChatOptions.Tools with nothing adapting it
        var functions = await Host().GetRequiredService<IAgentToolExecutor>()
            .GetAvailableFunctionsAsync(TestContext.Current.CancellationToken);

        var options = new Microsoft.Extensions.AI.ChatOptions { Tools = [.. functions] };

        Assert.Equal(functions.Count, options.Tools!.Count);
    }

    [Fact]
    public async Task GetAvailableFunctionsAsync_AFunctionInvokedLater_OpensAScopeOfItsOwn()
    {
        // a listing and a call are separated by however long a conversation takes, so the
        // function cannot hold the scope it was listed in
        var functions = await Host().GetRequiredService<IAgentToolExecutor>()
            .GetAvailableFunctionsAsync(TestContext.Current.CancellationToken);

        var add = functions.Single(function => function.Name == "probe_methods_add");

        var result = await add.InvokeAsync(
            new Microsoft.Extensions.AI.AIFunctionArguments { ["left"] = 2, ["right"] = 3 },
            TestContext.Current.CancellationToken);

        Assert.Equal(5, Assert.IsType<JsonElement>(result).GetInt32());
    }

    [Fact]
    public async Task MethodTakingTheContext_IsBoundFromTheCallAndLeftOutOfTheSchema()
    {
        // a model should never be asked to fill in who is calling
        var host = Host();

        var tools = await host.GetRequiredService<IAgentToolExecutor>().GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        var caller = tools.Single(tool => tool.Name == "probe_methods_caller");

        var properties = caller.Tool.JsonSchema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("note", out _));
        Assert.False(properties.TryGetProperty("context", out _));

        var result = await host.GetRequiredService<IAgentToolExecutor>()
            .InvokeCompleteAsync("probe_methods_caller", """{"note":"hi"}""", TestContext.Current.CancellationToken);

        Assert.Equal("\"nobody:hi\"", result);
    }

    [Fact]
    public async Task GetAvailableFunctionsAsync_CarriesTheAnnotationsTheEcosystemReads()
    {
        var functions = await Host().GetRequiredService<IAgentToolExecutor>()
            .GetAvailableFunctionsAsync(TestContext.Current.CancellationToken);

        var read = functions.Single(function => function.Name == "probe_scope_read");

        Assert.Equal(true, read.AdditionalProperties[AgentToolAnnotations.ReadOnlyHint]);
        Assert.Equal(false, read.AdditionalProperties[AgentToolAnnotations.DestructiveHint]);
        Assert.Equal("Read the scope", read.AdditionalProperties[AgentToolAnnotations.Title]);
    }
}
