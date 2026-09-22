using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Options;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Tests;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Tests.Adapters;

/// <summary>Tools adapted from a function rather than written as a class.</summary>
public class FunctionAgentToolTests
{
    private static AgentToolCreateOptions Options(
        string? name = null,
        string? title = null,
        string? description = null,
        bool isReadOnly = true,
        bool isDestructive = false,
        IReadOnlyDictionary<string, object?>? labels = null) =>
        new()
        {
            IsReadOnly = isReadOnly,
            IsDestructive = isDestructive,
            Name = name,
            Title = title,
            Description = description,
            Labels = labels
        };

    private static AIFunction Echo() => AIFunctionFactory.Create(
        ([Description("What to say back.")] string message) => new { echoed = message },
        "echo",
        "Says a message back.");

    [Fact]
    public void Create_TakesTheFunctionsNameSchemaAndProse()
    {
        var tool = AgentTool.Create(Echo(), Options());

        Assert.Equal("echo", tool.Name);
        Assert.Equal("Says a message back.", tool.Description);
        Assert.True(tool.InputSchema.GetProperty("properties").TryGetProperty("message", out _));
    }

    [Fact]
    public void Create_TitleDefaultsToTheName_BecauseAFunctionHasNone()
    {
        Assert.Equal("echo", AgentTool.Create(Echo(), Options()).Title);
    }

    [Fact]
    public void Create_NameAndDescriptionCanBeOverridden()
    {
        // what a host does to a tool whose original prose was not written for its model, or whose
        // name was chosen without knowing what it would sit beside
        var tool = AgentTool.Create(Echo(), Options(name: "probe_echo_say", description: "Repeats a message."));

        Assert.Equal("probe_echo_say", tool.Name);
        Assert.Equal("Repeats a message.", tool.Description);
    }

    [Fact]
    public void Create_ResultKindIsWhole()
    {
        Assert.Equal(AgentToolResultKind.Whole, AgentTool.Create(Echo(), Options()).ResultKind);
    }

    [Fact]
    public void Create_CarriesLabelsItWasHanded()
    {
        var tool = AgentTool.Create(Echo(), Options(labels: new Dictionary<string, object?> { ["tier"] = "external" }));

        Assert.True(tool.TryGetLabel<string>("tier", out var tier));
        Assert.Equal("external", tier);
    }

    [Fact]
    public void Create_FunctionWithNoReturnSchema_HasNoOutputSchema()
    {
        // the reason OutputSchema is nullable
        var voidFunction = AIFunctionFactory.Create(() => { }, "nothing", "Returns nothing.");

        Assert.Null(AgentTool.Create(voidFunction, Options()).OutputSchema);
    }

    [Fact]
    public async Task InvokeAsync_BindsArgumentsAndReturnsTheResult()
    {
        var tool = AgentTool.Create(Echo(), Options());

        var results = new List<object?>();

        await foreach (var item in tool.InvokeAsync(
            """{"message":"hello"}""", new AgentToolContext(), TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        Assert.Contains("hello", JsonSerializer.Serialize(Assert.Single(results)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsThatAreNotJson_ThrowsArgumentException()
    {
        var tool = AgentTool.Create(Echo(), Options());

        await Assert.ThrowsAsync<AgentToolArgumentException>(async () =>
        {
            await foreach (var _ in tool.InvokeAsync(
                "{ not json", new AgentToolContext(), TestContext.Current.CancellationToken))
            {
            }
        });
    }

    [Fact]
    public async Task InvokeAsync_PassesTheContextThroughToTheFunction()
    {
        // a function that needs what the adapter cannot project reaches for the context itself
        AgentToolContext? seen = null;

        var function = AIFunctionFactory.Create(
            (AIFunctionArguments arguments) =>
            {
                seen = arguments.Context?[typeof(AgentToolContext)] as AgentToolContext;

                return "ok";
            },
            "inspect",
            "Looks at its own invocation.");

        var tool = AgentTool.Create(function, Options());

        var context = new AgentToolContext();

        await foreach (var _ in tool.InvokeAsync(null, context, TestContext.Current.CancellationToken))
        {
        }

        Assert.Same(context, seen);
    }

    [Fact]
    public async Task Create_ToolIsComposedAndInvokedLikeAnyOther()
    {
        var registry = new AgentToolRegistry([StaticAgentToolSource.For(AgentTool.Create(Echo(), Options(name: "probe_echo_say")))]);
        var invoker = new AgentToolInvoker(registry, null);

        var json = await invoker.InvokeCompleteAsync(
            "probe_echo_say", """{"message":"hi"}""", new AgentToolContext(), TestContext.Current.CancellationToken);

        Assert.Contains("hi", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AsFunction_ReturnsWhatItWrapped()
    {
        var function = Echo();

        Assert.Same(function, AgentTool.Create(function, Options()).AsFunction());
    }
}
