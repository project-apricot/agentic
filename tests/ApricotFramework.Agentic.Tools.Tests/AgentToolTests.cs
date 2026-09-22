using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>Tool result kinds and schemas.</summary>
public class AgentToolTests
{
    private static AgentToolContext Caller() => new();

    [Fact]
    public void ResultKind_SingleTool_IsWhole()
    {
        Assert.Equal(AgentToolResultKind.Whole, new SingleProbe().ResultKind);
    }

    [Fact]
    public void ResultKind_SequenceTool_IsSequence()
    {
        Assert.Equal(AgentToolResultKind.Sequence, new SequenceProbe().ResultKind);
    }

    [Fact]
    public void OutputSchema_SequenceTool_DescribesTheAssembledArray()
    {
        var schema = new SequenceProbe().OutputSchema;

        Assert.Equal("array", schema!.Value.GetProperty("type").GetString());
    }

    [Fact]
    public void OutputSchema_ToolThatCanDescribeItsResult_IsNotNull()
    {
        // nullable because an adapted function returning nothing, or a foreign tool that
        // published none, has none to give
        Assert.NotNull(new SingleProbe().OutputSchema);
    }

    [Fact]
    public async Task InvokeAsync_SequenceTool_YieldsEachItem()
    {
        var items = new List<object?>();

        await foreach (var item in new SequenceProbe().InvokeAsync(null, Caller(), TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task InvokeAsync_NoArgumentsSupplied_ReadsAsEmptyObject()
    {
        var items = new List<object?>();

        await foreach (var item in new SequenceProbe().InvokeAsync("   ", Caller(), TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsThatAreNotJson_ThrowsArgumentException()
    {
        var tool = new SingleProbe();

        await Assert.ThrowsAsync<Exceptions.AgentToolArgumentException>(async () =>
        {
            await foreach (var _ in tool.InvokeAsync("{ not json", Caller(), TestContext.Current.CancellationToken))
            {
            }
        });
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsSupplied_AreBoundToTheDeclaredType()
    {
        var tool = new SingleProbe();

        await foreach (var item in tool.InvokeAsync("""{"id":42}""", Caller(), TestContext.Current.CancellationToken))
        {
            Assert.Equal(42, Assert.IsType<ProbeResult>(item).Id);
        }
    }
}
