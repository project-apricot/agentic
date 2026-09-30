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
        var tools = await Registry([new SingleProbe(), new SequenceProbe()]).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(2, tools.Count);
    }

    [Fact]
    public async Task GetToolsAsync_SeveralSources_ComposesThem()
    {
        var registry = new AgentToolRegistry(
            [StaticAgentToolSource.For(new SingleProbe()), StaticAgentToolSource.For(new SequenceProbe())]);

        Assert.Equal(2, (await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task GetToolsAsync_TwoSourcesOfferingOneName_Throws()
    {
        var registry = new AgentToolRegistry(
            [StaticAgentToolSource.For(new ConfigurableProbe("probe_items_get")),
             StaticAgentToolSource.For(new ConfigurableProbe("probe_items_get"))]);

        var exception = await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Contains("probe_items_get", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetToolsAsync_AskedTwice_AsksTheSourceAgain()
    {
        // the registry holds nothing between calls, because a source can answer differently for
        // a different caller or at a different moment - an upstream server whose tools changed,
        // or one connected per person. what that costs is the source's to cache, and a source
        // with a fixed list does it for free
        var source = new CountingSource([new SingleProbe()]);
        var registry = new AgentToolRegistry([source]);

        await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);
        await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);
        await registry.RequireAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(3, source.Calls);
    }

    [Fact]
    public async Task GetToolsAsync_AskedTwice_ChecksEachDeclarationOnce()
    {
        // composing per call would otherwise mean validating per call. keyed on the descriptor
        // itself, so a source handing back what it handed back before is not looked at again
        var source = new CountingSource([new SingleProbe()]);
        var counting = new CountingValidator();
        var registry = new AgentToolRegistry([source], [counting]);

        await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);
        await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(1, counting.Calls);
    }

    [Fact]
    public async Task GetToolsAsync_SourceAnsweringPerCaller_IsReflectedWithoutARestart()
    {
        var registry = new AgentToolRegistry([new PerCallerSource()]);

        var anonymous = await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        var named = await registry.GetToolsAsync(
            new AgentToolContext
            {
                Services = Probes.Services,
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity("probe"))
            },
            TestContext.Current.CancellationToken);

        Assert.Empty(anonymous);
        Assert.Single(named);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetToolsAsync_ToolWithoutName_Throws(string name)
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await Registry([new ConfigurableProbe(name)]).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetToolsAsync_ToolWithoutTitleOrDescription_IsAcceptedWithoutAValidator()
    {
        // the registry holds no opinions of its own beyond being able to address a tool
        var tools = await Registry([new ConfigurableProbe("probe_items_get", title: "  ", description: "  ")])
            .GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Single(tools);
    }

    [Fact]
    public async Task GetToolsAsync_HostValidatorRejects_Throws()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await Registry([new ConfigurableProbe("probe_items_get")], [new RejectEverything()]).GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetToolsAsync_FunctionDeclaringASequence_Throws()
    {
        // a function returns one value. a declaration claiming otherwise is not an opinion a host
        // might hold - it travels over the wire and misleads whoever acts on it
        var function = Microsoft.Extensions.AI.AIFunctionFactory.Create(() => Pair, "probe_pairs", "Returns two things.");

        var declaration = new AgentToolDeclaration
        {
            Name = "probe_pairs",
            Description = "Returns two things.",
            IsReadOnly = true,
            IsDestructive = false,
            ResultKind = AgentToolResultKind.Sequence
        };

        var registry = new AgentToolRegistry([new StaticAgentToolSource([new AgentToolDescriptor(function, declaration)])]);

        var exception = await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Contains("cannot produce items", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetToolsAsync_AgentToolDeclaringASequence_IsAccepted()
    {
        // only an AgentTool can produce items, and this one does
        var tools = await new AgentToolRegistry([StaticAgentToolSource.For(new SequenceProbe())])
            .GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(AgentToolResultKind.Sequence, Assert.Single(tools).Declaration.ResultKind);
    }

    [Fact]
    public async Task RequireAsync_UnknownName_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<AgentToolNotFoundException>(
            async () => await Registry([new SingleProbe()]).RequireAsync("probe_items_obliterate", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_UnknownName_ReturnsNull()
    {
        Assert.Null(await Registry([new SingleProbe()]).FindAsync("probe_items_obliterate", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_NoName_ReturnsNull()
    {
        Assert.Null(await Registry([new SingleProbe()]).FindAsync(null, Probes.Context(), TestContext.Current.CancellationToken));
    }

    /// <summary>A source that counts how often it is asked.</summary>
    /// <param name="tools">The tools to offer.</param>
    private sealed class CountingSource(IEnumerable<AgentTool> tools) : IAgentToolSource
    {
        private readonly IReadOnlyList<AgentToolDescriptor> tools = [.. tools.Select(tool => new AgentToolDescriptor(tool))];

        /// <summary>Gets how many times it was asked.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return ValueTask.FromResult(this.tools);
        }
    }

    /// <summary>Something for a function to return.</summary>
    private static readonly string[] Pair = ["one", "two"];

    /// <summary>A source that offers a tool only to a caller it knows.</summary>
    private sealed class PerCallerSource : IAgentToolSource
    {
        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AgentToolDescriptor> tools = context.User?.Identity?.IsAuthenticated == true
                ? [new AgentToolDescriptor(new SingleProbe())]
                : [];

            return ValueTask.FromResult(tools);
        }
    }

    /// <summary>A host check that counts how often it is asked.</summary>
    private sealed class CountingValidator : IAgentToolValidator
    {
        /// <summary>Gets how many times it was asked.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public void Validate(AgentToolDescriptor tool) => this.Calls++;
    }

    /// <summary>A host check that refuses everything.</summary>
    private sealed class RejectEverything : IAgentToolValidator
    {
        /// <inheritdoc />
        public void Validate(AgentToolDescriptor tool) => throw new AgentToolDeclarationException("no");
    }
}
