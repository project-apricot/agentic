using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>Tool result kinds and schemas.</summary>
public class AgentToolTests
{
    private static AgentToolContext Caller() => Probes.Context();

    private static Microsoft.Extensions.AI.AIFunctionArguments Arguments(string? json = null) =>
        AgentToolInvocation.Create(json, Caller());

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
        var schema = new SequenceProbe().ReturnJsonSchema;

        Assert.Equal("array", schema!.Value.GetProperty("type").GetString());
    }

    [Fact]
    public void OutputSchema_ToolThatCanDescribeItsResult_IsNotNull()
    {
        // nullable because an adapted function returning nothing, or a foreign tool that
        // published none, has none to give
        Assert.NotNull(new SingleProbe().ReturnJsonSchema);
    }

    [Fact]
    public async Task InvokeAsync_SequenceTool_YieldsEachItem()
    {
        var items = new List<object?>();

        await foreach (var item in new SequenceProbe().InvokeStreamingAsync(Arguments(), TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task InvokeAsync_NoArgumentsSupplied_ReadsAsEmptyObject()
    {
        var items = new List<object?>();

        await foreach (var item in new SequenceProbe().InvokeStreamingAsync(Arguments("   "), TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        Assert.NotEmpty(items);
    }

    [Fact]
    public void Arguments_ThatAreNotJson_ThrowArgumentException()
    {
        Assert.Throws<Exceptions.AgentToolArgumentException>(() => Arguments("{ not json"));
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsOfTheWrongShape_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<Exceptions.AgentToolArgumentException>(async () =>
            await new SingleProbe().InvokeAsync(Arguments("""{"id":"not a number"}"""), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsSupplied_AreBoundToTheDeclaredType()
    {
        var result = await new SingleProbe().InvokeAsync(Arguments("""{"id":42}"""), TestContext.Current.CancellationToken);

        Assert.Equal(42, Assert.IsType<ProbeResult>(result).Id);
    }
}
