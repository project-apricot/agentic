using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;

namespace ApricotFramework.Agentic.Tools.Tests.Registry;

/// <summary>Composing sources, and the checks applied as they are composed.</summary>
public class AgentToolRegistryTests
{
    private static AgentToolRegistry Registry(IEnumerable<AgentTool> tools, IEnumerable<IAgentToolValidator>? validators = null) =>
        new([new StaticAgentToolSource([.. tools.Select(tool => new AgentToolDescriptor(tool))])], validators);

    [Fact]
    public async Task GetToolsAsync_WellFormedTools_OffersThemAll()
    {
        var tools = await Registry([new SingleProbe(), new SequenceProbe()]).GetToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, tools.Count);
    }

    [Fact]
    public async Task GetToolsAsync_SeveralSources_ComposesThem()
    {
        var registry = new AgentToolRegistry(
            [StaticAgentToolSource.For(new SingleProbe()), StaticAgentToolSource.For(new SequenceProbe())]);

        Assert.Equal(2, (await registry.GetToolsAsync(TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task GetToolsAsync_TwoSourcesOfferingOneName_Throws()
    {
        var registry = new AgentToolRegistry(
            [StaticAgentToolSource.For(new ConfigurableProbe("probe_items_get")),
             StaticAgentToolSource.For(new ConfigurableProbe("probe_items_get"))]);

        var exception = await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(TestContext.Current.CancellationToken));

        Assert.Contains("probe_items_get", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetToolsAsync_AskedTwice_ComposesOnce()
    {
        var source = new CountingSource([new SingleProbe()]);
        var registry = new AgentToolRegistry([source]);

        await registry.GetToolsAsync(TestContext.Current.CancellationToken);
        await registry.GetToolsAsync(TestContext.Current.CancellationToken);
        await registry.RequireAsync("probe_items_get", TestContext.Current.CancellationToken);

        Assert.Equal(1, source.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetToolsAsync_ToolWithoutName_Throws(string name)
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await Registry([new ConfigurableProbe(name)]).GetToolsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetToolsAsync_ToolWithoutTitleOrDescription_IsAcceptedWithoutAValidator()
    {
        // the registry holds no opinions of its own beyond being able to address a tool
        var tools = await Registry([new ConfigurableProbe("probe_items_get", title: "  ", description: "  ")])
            .GetToolsAsync(TestContext.Current.CancellationToken);

        Assert.Single(tools);
    }

    [Fact]
    public async Task GetToolsAsync_HostValidatorRejects_Throws()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await Registry([new ConfigurableProbe("probe_items_get")], [new RejectEverything()]).GetToolsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RequireAsync_UnknownName_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<AgentToolNotFoundException>(
            async () => await Registry([new SingleProbe()]).RequireAsync("probe_items_obliterate", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_UnknownName_ReturnsNull()
    {
        Assert.Null(await Registry([new SingleProbe()]).FindAsync("probe_items_obliterate", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_NoName_ReturnsNull()
    {
        Assert.Null(await Registry([new SingleProbe()]).FindAsync(null, TestContext.Current.CancellationToken));
    }

    /// <summary>A source that counts how often it is asked.</summary>
    /// <param name="tools">The tools to offer.</param>
    private sealed class CountingSource(IEnumerable<AgentTool> tools) : IAgentToolSource
    {
        private readonly IReadOnlyList<AgentToolDescriptor> tools = [.. tools.Select(tool => new AgentToolDescriptor(tool))];

        /// <summary>Gets how many times it was asked.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return ValueTask.FromResult(this.tools);
        }
    }

    /// <summary>A host check that refuses everything.</summary>
    private sealed class RejectEverything : IAgentToolValidator
    {
        /// <inheritdoc />
        public void Validate(AgentToolDescriptor tool) => throw new AgentToolDeclarationException("no");
    }
}
