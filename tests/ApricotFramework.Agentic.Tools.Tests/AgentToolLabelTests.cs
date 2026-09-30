using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>Host-defined labels, and the vocabulary staying the host's.</summary>
public class AgentToolLabelTests
{
    /// <summary>A vocabulary a host might define. Nothing in the library knows it exists.</summary>
    private enum Sensitivity
    {
        Public = 0,
        Personal = 1
    }

    [AgentToolLabel("tier", "standard")]
    [AgentToolLabel("sensitivity", Sensitivity.Public)]
    private abstract class LabelledBase : ProbeTool<ProbeNoArguments, string>
    {
        public override string Title => "Probe";

        public override string Description => "Does a thing.";

        public override bool IsReadOnly => true;

        public override bool IsDestructive => false;


        protected override Task<string> ExecuteAsync(ProbeNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
    }

    [AgentToolLabel("sensitivity", Sensitivity.Personal)]
    private sealed class LabelledProbe : LabelledBase
    {
        public override string Name => "probe_items_labelled";
    }

    private sealed class UnlabelledProbe : LabelledBase
    {
        public override string Name => "probe_items_plain";
    }

    /// <summary>A tool built at run time, carrying labels it was handed rather than declared.</summary>
    private sealed class ConstructedProbe(IReadOnlyDictionary<string, object?> labels) : LabelledBase
    {
        public override string Name => "probe_items_constructed";

        public override IReadOnlyDictionary<string, object?> Labels => labels;
    }

    [Fact]
    public void Labels_DeclaredOnTheTool_AreRead()
    {
        Assert.True(new LabelledProbe().TryGetLabel<Sensitivity>("sensitivity", out var value));
        Assert.Equal(Sensitivity.Personal, value);
    }

    [Fact]
    public void Labels_DeclaredOnABaseClass_AreInherited()
    {
        Assert.True(new LabelledProbe().TryGetLabel<string>("tier", out var value));
        Assert.Equal("standard", value);
    }

    [Fact]
    public void Labels_RestatedOnTheDerivedTool_OverrideTheFamily()
    {
        Assert.True(new UnlabelledProbe().TryGetLabel<Sensitivity>("sensitivity", out var inherited));
        Assert.Equal(Sensitivity.Public, inherited);

        Assert.True(new LabelledProbe().TryGetLabel<Sensitivity>("sensitivity", out var overridden));
        Assert.Equal(Sensitivity.Personal, overridden);
    }

    [Fact]
    public void Labels_ToolCarryingNone_IsEmpty()
    {
        Assert.Empty(new SingleProbe().Labels);
        Assert.False(new SingleProbe().HasLabel("sensitivity"));
    }

    [Fact]
    public void Labels_WrongType_IsNotRead()
    {
        // asking for the wrong type reports absence rather than throwing, so a policy that does
        // not recognise a value fails closed rather than falling over
        Assert.False(new LabelledProbe().TryGetLabel<int>("sensitivity", out _));
    }

    [Fact]
    public void Labels_SuppliedRatherThanDeclared_AreCarriedToo()
    {
        // the path a tool adapted from a function or a foreign server takes
        var tool = new ConstructedProbe(new Dictionary<string, object?> { ["sensitivity"] = Sensitivity.Personal });

        Assert.True(tool.TryGetLabel<Sensitivity>("sensitivity", out var value));
        Assert.Equal(Sensitivity.Personal, value);
    }

    [Fact]
    public void HasLabel_NamePresentWithoutValue_IsStillCarried()
    {
        var tool = new ConstructedProbe(new Dictionary<string, object?> { ["experimental"] = null });

        Assert.True(tool.HasLabel("experimental"));
    }

    [Fact]
    public async Task TypedLabel_DerivedFromTheLabelAttribute_IsReadAsALabel()
    {
        // a host closing its vocabulary at the declaration site: no label name spelled out, no
        // value that is not one of the enum's
        var tools = await TypedTools();

        var family = tools.Single(tool => tool.Name == "probe_typed_family");

        Assert.True(family.Declaration.TryGetLabel<Confidentiality>(TypedLabelNames.Confidentiality, out var level));
        Assert.Equal(Confidentiality.Public, level);
    }

    [Fact]
    public async Task TypedLabel_OnAMethod_NarrowsItsFamily()
    {
        var tools = await TypedTools();

        var narrowed = tools.Single(tool => tool.Name == "probe_typed_narrowed");

        Assert.True(narrowed.Declaration.TryGetLabel<Confidentiality>(TypedLabelNames.Confidentiality, out var level));
        Assert.Equal(Confidentiality.Personal, level);
    }

    [Fact]
    public async Task TypedLabel_ReadAsMetadata_AccumulatesRatherThanOverriding()
    {
        // the difference worth knowing: labels override by name, metadata accumulates in the
        // order it was said - the holding class first, the method after. A host reading a typed
        // label out of the metadata has to take the last, which is why reading it as a label is
        // the better habit
        var tools = await TypedTools();

        var declared = tools.Single(tool => tool.Name == "probe_typed_narrowed").GetMetadata<ConfidentialityAttribute>();

        Assert.Equal([Confidentiality.Public, Confidentiality.Personal], declared.Select(attribute => attribute.Level));
    }

    /// <summary>Composes the tools carrying a typed label.</summary>
    /// <returns>A task containing the tools.</returns>
    private static async Task<IReadOnlyList<AgentToolDescriptor>> TypedTools()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        services.AddAgentToolsCore();
        services.AddAgentToolType<TypedLabelProbes>();

        var host = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);

        return await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<IAgentToolRegistry>(host)
            .GetToolsAsync(new AgentToolContext { Services = host }, TestContext.Current.CancellationToken);
    }
}
