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
    private abstract class LabelledBase : AgentTool<AgentToolNoArguments, string>
    {
        public override string Title => "Probe";

        public override string Description => "Does a thing.";

        public override bool IsReadOnly => true;

        public override bool IsDestructive => false;


        protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken) => Task.FromResult("ok");
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
}
