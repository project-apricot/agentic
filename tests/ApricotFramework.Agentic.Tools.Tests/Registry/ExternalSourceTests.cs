using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;

namespace ApricotFramework.Agentic.Tools.Tests.Registry;

/// <summary>A tool from somewhere this host does not control costs that tool, never the rest.</summary>
public class ExternalSourceTests
{
    [Fact]
    public async Task ExternalToolAValidatorRefuses_IsLeftOut_AndTheRestAreOffered()
    {
        var registry = new AgentToolRegistry(
            [StaticAgentToolSource.For(new SingleProbe()), new External(StaticAgentToolSource.For(new SequenceProbe()))],
            [new RefusingNamed("probe_items_list")]);

        var tools = await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(["probe_items_get"], tools.Select(tool => tool.Name));
    }

    [Fact]
    public async Task OwnToolAValidatorRefuses_StillThrows()
    {
        var registry = new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe())], [new RefusingNamed("probe_items_get")]);

        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExternalToolUnderATakenName_IsLeftOut_AndThisHostsOwnWins_WhateverTheOrder()
    {
        var own = new SingleProbe();

        // the external source is registered first, and still loses the name
        var registry = new AgentToolRegistry([new External(StaticAgentToolSource.For(new SingleProbe())), StaticAgentToolSource.For(own)]);

        var tools = await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Same(own, Assert.Single(tools).Tool);
        Assert.Same(own, (await registry.FindAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken))!.Tool);
    }

    [Fact]
    public async Task ExternalSourceThatFails_CostsItsTools_AndNothingElse()
    {
        var registry = new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe()), new External(new Failing())]);

        var tools = await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Equal(["probe_items_get"], tools.Select(tool => tool.Name));
        Assert.Null(await registry.FindAsync("anything", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OwnSourceThatFails_StillThrows()
    {
        var registry = new AgentToolRegistry([new Failing()]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindAsync_StopsAtTheFirstSourceOfferingTheName()
    {
        var later = new Counting(StaticAgentToolSource.For(new SequenceProbe()));

        var registry = new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe()), later]);

        var found = await registry.FindAsync("probe_items_get", Probes.Context(), TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(0, later.Calls);
    }

    [Fact]
    public async Task CuratingAnExternalSource_KeepsItExternal()
    {
        var curated = new CuratingAgentToolSource(new External(StaticAgentToolSource.For(new SingleProbe())), tool => tool);

        Assert.True(((IAgentToolSource)curated).IsExternal);

        var registry = new AgentToolRegistry([curated], [new RefusingNamed("probe_items_get")]);

        Assert.Empty(await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken));
    }

    private sealed class External(IAgentToolSource inner) : IAgentToolSource
    {
        public bool IsExternal => true;

        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default) =>
            inner.GetToolsAsync(context, cancellationToken);

        public ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default) =>
            inner.FindAsync(name, context, cancellationToken);
    }

    private sealed class Failing : IAgentToolSource
    {
        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("unreachable");

        public ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("unreachable");
    }

    private sealed class Counting(IAgentToolSource inner) : IAgentToolSource
    {
        public int Calls { get; private set; }

        public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return inner.GetToolsAsync(context, cancellationToken);
        }

        public ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return inner.FindAsync(name, context, cancellationToken);
        }
    }

    private sealed class RefusingNamed(string name) : IAgentToolValidator
    {
        public void Validate(AgentToolDescriptor tool)
        {
            if (tool.Name == name)
            {
                throw new AgentToolDeclarationException("refused");
            }
        }
    }
}
