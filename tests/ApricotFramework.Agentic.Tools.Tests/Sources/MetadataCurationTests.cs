using ApricotFramework.Agentic.Tools.Sources;

namespace ApricotFramework.Agentic.Tools.Tests.Sources;

/// <summary>Adding to what is said about a tool without losing what was said already.</summary>
public class MetadataCurationTests
{
    private static AgentToolDescriptor Described() => new(new SingleProbe(), ["first"]);

    [Fact]
    public void WithMetadata_AddsAfterWhatIsThere()
    {
        Assert.Equal(["first", "second"], Described().WithMetadata("second").Metadata);
    }

    [Fact]
    public void AddingFor_DecidesFromTheToolItself_AndKeepsWhatIsThere()
    {
        var curate = AgentToolCuration.AddingFor(tool => [tool.Declaration.IsReadOnly ? "read" : "write"]);

        Assert.Equal(["first", "read"], curate(Described())!.Metadata);
    }
}
