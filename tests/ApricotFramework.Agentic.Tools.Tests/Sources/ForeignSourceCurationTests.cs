using ApricotFramework.Agentic.Tools.Adapters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Validators;

namespace ApricotFramework.Agentic.Tools.Tests.Sources;

/// <summary>A source a host does not control, and what it can do about what it says.</summary>
public class ForeignSourceCurationTests
{
    private static readonly IAgentToolValidator[] Checks = [new DescriptionDeclaredValidator()];

    private static AgentToolRegistry Registry(IAgentToolSource source) => new([source], Checks);

    /// <summary>A foreign server offering one good tool and one it got wrong.</summary>
    private static StaticAgentToolSource Foreign() =>
        StaticAgentToolSource.For(new ConfigurableProbe("search"), new ConfigurableProbe("broken", description: "  "));

    [Fact]
    public async Task Uncurated_OneMalformedTool_TakesTheWholeRegistryWithIt()
    {
        // the behaviour worth knowing before reaching for a curator
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await Registry(Foreign()).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DropRejected_MalformedToolCostsOnlyItself()
    {
        var dropped = new List<string>();

        var source = new CuratingAgentToolSource(
            Foreign(),
            AgentToolCuration.DropRejected(Checks, (tool, _) => dropped.Add(tool.Name)));

        var tools = await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal("search", Assert.Single(tools).Name);
        Assert.Equal(["broken"], dropped);
    }

    [Fact]
    public async Task DropRejected_ReportsWhyItDropped()
    {
        AgentToolDeclarationException? reason = null;

        var source = new CuratingAgentToolSource(
            Foreign(),
            AgentToolCuration.DropRejected(Checks, (_, exception) => reason = exception));

        await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Contains("description", reason!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Prefixing_KeepsTwoSourcesFromColliding()
    {
        // two foreign servers both offering "search"
        var registry = new AgentToolRegistry(
            [
                new CuratingAgentToolSource(StaticAgentToolSource.For(new ConfigurableProbe("search")), AgentToolCuration.Prefixing("weather_")),
                new CuratingAgentToolSource(StaticAgentToolSource.For(new ConfigurableProbe("search")), AgentToolCuration.Prefixing("docs_"))
            ],
            Checks);

        var tools = await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(["docs_search", "weather_search"], tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task WithoutPrefixing_TwoSourcesOfferingOneName_StillRefuses()
    {
        // a collision is genuinely ambiguous, so no per-tool check finds it and the registry does
        // not pick one
        var registry = new AgentToolRegistry(
            [
                new CuratingAgentToolSource(StaticAgentToolSource.For(new ConfigurableProbe("search")), AgentToolCuration.DropRejected(Checks)),
                new CuratingAgentToolSource(StaticAgentToolSource.For(new ConfigurableProbe("search")), AgentToolCuration.DropRejected(Checks))
            ],
            Checks);

        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Where_OffersOnlyWhatWasReviewed()
    {
        var source = new CuratingAgentToolSource(
            StaticAgentToolSource.For(new ConfigurableProbe("search"), new ConfigurableProbe("delete_everything")),
            AgentToolCuration.Where(tool => tool.Name == "search"));

        Assert.Equal("search", Assert.Single(await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken)).Name);
    }

    [Fact]
    public async Task Adding_AttachesWhatAForeignToolCouldNotCarry()
    {
        // nothing a host fetches carries an authorization attribute, so whatever should gate it is
        // attached by whoever fetched it
        var marker = new object();

        var source = new CuratingAgentToolSource(Foreign(), AgentToolCuration.Adding(marker));

        var tools = await source.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.All(tools, tool => Assert.Contains(marker, tool.Metadata));
    }

    [Fact]
    public async Task Curators_Nest_AndApplyInnermostFirst()
    {
        // prefix everything, then drop whatever still would not pass
        var dropped = new List<string>();

        var source = new CuratingAgentToolSource(
            new CuratingAgentToolSource(Foreign(), AgentToolCuration.Prefixing("weather_")),
            AgentToolCuration.DropRejected(Checks, (tool, _) => dropped.Add(tool.Name)));

        var tools = await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal("weather_search", Assert.Single(tools).Name);
        Assert.Equal(["weather_broken"], dropped);
    }

    [Fact]
    public async Task Renaming_LeavesEverythingElseAlone()
    {
        var source = new CuratingAgentToolSource(
            StaticAgentToolSource.For(new ConfigurableProbe("search", title: "Search the web")),
            AgentToolCuration.Prefixing("weather_"));

        var tool = Assert.Single(await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal("weather_search", tool.Name);
        // the title is a label a person reads and stays what the tool called itself
        Assert.Equal("Search the web", tool.Declaration.Title);
    }

    [Fact]
    public async Task Fixing_ADeclarationAHostCouldOtherwiseNotAccept()
    {
        // the other half: a tool that would be refused, offered on terms the host sets
        var source = new CuratingAgentToolSource(
            StaticAgentToolSource.For(new ConfigurableProbe("broken", description: "  ")),
            tool => tool.With(AgentToolDeclaration.From(tool.Declaration) with { Description = "Searches a third party index." }));

        var tool = Assert.Single(await Registry(source).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal("Searches a third party index.", tool.Declaration.Description);
    }

    [Fact]
    public async Task Fixed_ToolStillRuns()
    {
        var source = new CuratingAgentToolSource(
            StaticAgentToolSource.For(new ConfigurableProbe("broken", description: "  ")),
            tool => tool.With(AgentToolDeclaration.From(tool.Declaration) with { Description = "Searches." }));

        var invoker = new AgentToolInvoker(Registry(source));

        var result = await invoker.InvokeCompleteAsync("broken", null, Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal("\"ok\"", result);
    }

}
