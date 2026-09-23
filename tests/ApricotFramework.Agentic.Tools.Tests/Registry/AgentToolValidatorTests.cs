using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Validators;

namespace ApricotFramework.Agentic.Tools.Tests.Registry;

/// <summary>The checks this library ships, none of which it applies unasked.</summary>
public class AgentToolValidatorTests
{
    private static async Task<IReadOnlyList<AgentToolDescriptor>> Compose(AgentTool tool, params IAgentToolValidator[] validators)
    {
        var registry = new AgentToolRegistry([StaticAgentToolSource.For(tool)], validators);

        return await registry.GetToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WithNoValidators_AToolMissingEverythingButANameIsAccepted()
    {
        // the registry insists on a name because it cannot address a tool without one, and on
        // nothing else because nothing else stops it working
        Assert.Single(await Compose(new ConfigurableProbe("probe_items_get", title: "  ", description: "  ")));
    }

    [Fact]
    public async Task TitleDeclaredValidator_RefusesAToolWithout()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            () => Compose(new ConfigurableProbe("probe_items_get", title: "  "), new TitleDeclaredValidator()));
    }

    [Fact]
    public async Task TitleDeclaredValidator_AcceptsAToolWithOne()
    {
        Assert.Single(await Compose(new ConfigurableProbe("probe_items_get"), new TitleDeclaredValidator()));
    }

    [Fact]
    public async Task DescriptionDeclaredValidator_RefusesAToolWithout()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            () => Compose(new ConfigurableProbe("probe_items_get", description: "  "), new DescriptionDeclaredValidator()));
    }

    [Fact]
    public async Task ConsistentBehaviourValidator_RefusesAContradiction()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            () => Compose(new ConfigurableProbe("probe_items_get", readOnly: true, destructive: true), new ConsistentBehaviourValidator()));
    }

    [Fact]
    public async Task ConsistentBehaviourValidator_WithoutIt_AContradictionIsAccepted()
    {
        // not a special case: nothing here decides what a host checks
        Assert.Single(await Compose(new ConfigurableProbe("probe_items_get", readOnly: true, destructive: true)));
    }

    [Fact]
    public async Task HostValidator_RunsBesideTheShippedOnes()
    {
        await Assert.ThrowsAsync<AgentToolDeclarationException>(
            () => Compose(new ConfigurableProbe("probe_items_get"), new TitleDeclaredValidator(), new RejectEverything()));
    }

    /// <summary>A host check that refuses everything.</summary>
    private sealed class RejectEverything : IAgentToolValidator
    {
        /// <inheritdoc />
        public void Validate(AgentToolDescriptor tool) => throw new AgentToolDeclarationException("no");
    }
}
