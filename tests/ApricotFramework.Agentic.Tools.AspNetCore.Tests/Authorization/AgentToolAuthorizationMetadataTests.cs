using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Authorization;

/// <summary>Reading a tool's requirements from its attributes.</summary>
public class AgentToolAuthorizationMetadataTests
{
    [Fact]
    public void For_ToolWithAttribute_ReadsItsRequirement()
    {
        var names = AgentToolAuthorizationMetadata.For(new AgentToolDescriptor(new GatedProbe(), typeof(GatedProbe).GetCustomAttributes(inherit: true))).DeclaredRequirements.OfType<ProbeRequirement>().Select(requirement => requirement.Name);

        Assert.Contains("granted", names);
    }

    [Fact]
    public void For_AttributeOnBaseClass_IsInherited()
    {
        // a family of tools can declare a shared requirement once
        var names = AgentToolAuthorizationMetadata.For(new AgentToolDescriptor(new GatedProbe(), typeof(GatedProbe).GetCustomAttributes(inherit: true))).DeclaredRequirements.OfType<ProbeRequirement>().Select(requirement => requirement.Name).ToList();

        Assert.Contains("inherited", names);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public void For_ToolWithoutAttribute_IsEmpty()
    {
        Assert.Empty(AgentToolAuthorizationMetadata.For(new AgentToolDescriptor(new UngatedProbe(), typeof(UngatedProbe).GetCustomAttributes(inherit: true))).DeclaredRequirements);
    }

    [Fact]
    public void For_ReadsWhatTheDescriptorCarries_NotTheTypeItself()
    {
        // the descriptor is the single source: a tool registered with nothing said about it
        // carries nothing, even though its type has attributes
        Assert.Empty(AgentToolAuthorizationMetadata.For(new AgentToolDescriptor(new GatedProbe())).DeclaredRequirements);
    }

    [Fact]
    public void For_PolicyInTheMetadata_ReadsItsRequirements()
    {
        // a descriptor built elsewhere - a foreign source, a host's own wiring - can carry a
        // policy object rather than an attribute, and it counts the same. the MCP SDK reads the
        // same three shapes
        var policy = new AuthorizationPolicyBuilder().AddRequirements(new ProbeRequirement("from-policy")).Build();

        var declared = AgentToolAuthorizationMetadata.For(new AgentToolDescriptor(new UngatedProbe(), [policy]));

        Assert.True(declared.IsDeclared);
        Assert.Equal(["from-policy"], declared.DeclaredRequirements.OfType<ProbeRequirement>().Select(requirement => requirement.Name));
    }

    [Fact]
    public void For_AllowAnonymous_WaivesWhateverElseIsDeclared()
    {
        // read as "nothing governs this tool", which is what both opens it and keeps the tripwire
        // quiet about a gate the host waived on purpose
        var declared = AgentToolAuthorizationMetadata.For(
            new AgentToolDescriptor(new AnonymousProbe(), typeof(AnonymousProbe).GetCustomAttributes(inherit: true)));

        Assert.True(declared.AllowsAnonymous);
        Assert.False(declared.IsDeclared);
        Assert.NotEmpty(declared.DeclaredRequirements);
    }
}
