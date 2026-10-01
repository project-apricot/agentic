using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Tests.Sources;

/// <summary>A source with a fixed list answers a lookup from its index, and agrees with its listing.</summary>
public class SourceLookupTests
{
    [Fact]
    public async Task AStaticSource_FindsWhatItLists_AndNothingElse()
    {
        var source = StaticAgentToolSource.For(new SingleProbe(), new SequenceProbe());

        var listed = await source.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Same(listed[0], await source.FindAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken));
        Assert.Null(await source.FindAsync("probe_items_missing", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AStaticSource_OfferingANameTwice_FindsTheFirst_AsTheRegistryWouldOfferIt()
    {
        var first = new SingleProbe();

        var source = StaticAgentToolSource.For(first, new SingleProbe());

        Assert.Same(first, (await source.FindAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken))!.Tool);
    }

    [Fact]
    public async Task TheRegistrationSource_FindsFromWhatItDescribedOnce()
    {
        var services = new ServiceCollection();

        services.AddScoped<ScopedDependency>();
        services.AddAgentToolsCore();
        services.AddAgentToolType<MethodProbes>();

        await using var host = services.BuildServiceProvider();
        await using var scope = host.CreateAsyncScope();

        var source = host.GetServices<IAgentToolSource>().OfType<RegistrationAgentToolSource>().Single();
        var context = new AgentToolContext { Services = scope.ServiceProvider };

        var found = await source.FindAsync("probe_methods_add", context, TestContext.Current.CancellationToken);
        var listed = await source.GetToolsAsync(context, TestContext.Current.CancellationToken);

        Assert.Same(listed.Single(tool => tool.Name == "probe_methods_add"), found);
    }
}
