using ApricotFramework.Agentic.Tools.Discovery;
using ApricotFramework.Agentic.Tools.Tests.Discovery;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>Finding tools by looking for them.</summary>
public class AgentToolDiscoveryTests
{
    private static readonly Assembly Here = typeof(DiscoveredOneTool).Assembly;

    private static List<Type> Found() =>
        AgentToolDiscovery.FromAssembly(Here, type => type.Namespace == typeof(DiscoveredOneTool).Namespace).ToList();

    [Fact]
    public void FromAssembly_FindsConcreteTools()
    {
        Assert.Contains(typeof(DiscoveredOneTool), Found());
        Assert.Contains(typeof(DiscoveredTwoTool), Found());
    }

    [Fact]
    public void FromAssembly_SkipsAnAbstractBase()
    {
        // the base is the family, not a member of it
        Assert.DoesNotContain(typeof(DiscoverableToolBase), Found());
    }

    [Fact]
    public void FromAssembly_SkipsAnOpenGeneric()
    {
        Assert.DoesNotContain(typeof(OpenGenericTool<>), Found());
    }

    [Fact]
    public void FromAssembly_SkipsAnIgnoredTool()
    {
        Assert.DoesNotContain(typeof(IgnoredTool), Found());
    }

    [Fact]
    public void FromAssembly_FindsAnInternalTool()
    {
        // a host may reasonably keep its tools internal
        Assert.Contains(typeof(InternalTool), Found());
    }

    [Fact]
    public void FromAssembly_PredicateNarrowsFurther()
    {
        var narrowed = AgentToolDiscovery.FromAssembly(Here, type => type == typeof(DiscoveredOneTool)).ToList();

        Assert.Equal(typeof(DiscoveredOneTool), Assert.Single(narrowed));
    }

    [Fact]
    public void FromAssembly_WithoutAPredicate_FindsEveryToolInTheAssembly()
    {
        // including the ones written for other tests, which is the hazard the predicate answers
        var all = AgentToolDiscovery.FromAssembly(Here).ToList();

        Assert.Contains(typeof(SingleProbe), all);
        Assert.True(all.Count > Found().Count);
    }

    [Fact]
    public void FromAssemblies_ReportsEachToolOnce()
    {
        var found = AgentToolDiscovery.FromAssemblies([Here, Here], type => type == typeof(DiscoveredOneTool)).ToList();

        Assert.Single(found);
    }

    [Theory]
    [InlineData(typeof(DiscoveredOneTool), true)]
    [InlineData(typeof(DiscoverableToolBase), false)]
    [InlineData(typeof(IgnoredTool), false)]
    [InlineData(typeof(OpenGenericTool<>), false)]
    [InlineData(typeof(AgentToolDiscoveryTests), false)]
    public void IsTool_AnswersForOneType(Type type, bool expected)
    {
        Assert.Equal(expected, AgentToolDiscovery.IsTool(type));
    }
}
