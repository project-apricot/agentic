using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Tests.Functions;

/// <summary>
/// A tool that is already an <see cref="AIFunction"/> - a delegate, something built from
/// configuration, a tool read off a foreign MCP server.
/// </summary>
/// <remarks>
/// Nothing wraps it. The declaration sits beside the function in the descriptor, which is what
/// keeps the function's own identity reachable - the property this shape exists for.
/// </remarks>
public class ForeignFunctionTests
{
    private static AIFunction Echo() => AIFunctionFactory.Create(
        ([Description("What to say back.")] string message) => new { echoed = message },
        "echo",
        "Says a message back.");

    private static AgentToolDeclaration Declaration(string name = "echo", string? description = null, IReadOnlyDictionary<string, object?>? labels = null) =>
        new()
        {
            Name = name,
            Description = description ?? "Says a message back.",
            IsReadOnly = true,
            IsDestructive = false,
            Labels = labels ?? AgentToolLabels.None
        };

    [Fact]
    public void Descriptor_KeepsTheFunctionAsItArrived()
    {
        // the reason the declaration is not on the tool: a consumer reaching through a listed
        // tool for what is really behind it - an McpClientTool, say - finds it
        var function = Echo();

        var descriptor = new AgentToolDescriptor(function, Declaration());

        Assert.Same(function, descriptor.Tool);
        Assert.Same(function, descriptor.AsFunction());
    }

    [Fact]
    public void Descriptor_TakesSchemasFromTheFunction()
    {
        var descriptor = new AgentToolDescriptor(Echo(), Declaration());

        Assert.True(descriptor.Tool.JsonSchema.GetProperty("properties").TryGetProperty("message", out _));
    }

    [Fact]
    public void Declaration_TitleFallsBackToTheName_BecauseAFunctionHasNone()
    {
        Assert.Equal("echo", Declaration().Title);
    }

    [Fact]
    public void Declaration_IsWhereARenameHappens()
    {
        // a foreign name chosen without knowing what it would sit beside, offered differently -
        // with the same function underneath
        var function = Echo();

        var descriptor = new AgentToolDescriptor(function, Declaration(name: "probe_echo_say", description: "Repeats a message."));

        Assert.Equal("probe_echo_say", descriptor.Name);
        Assert.Equal("Repeats a message.", descriptor.Declaration.Description);
        Assert.Equal("echo", descriptor.Tool.Name);
        Assert.Same(function, descriptor.Tool);
    }

    [Fact]
    public void Declaration_ResultKindIsWhole()
    {
        // a function returns one value, whatever else is said about it
        Assert.Equal(AgentToolResultKind.Whole, Declaration().ResultKind);
    }

    [Fact]
    public void Declaration_CarriesLabelsItWasHanded()
    {
        var declaration = Declaration(labels: new Dictionary<string, object?> { ["tier"] = "external" });

        Assert.True(declaration.TryGetLabel<string>("tier", out var tier));
        Assert.Equal("external", tier);
    }

    [Fact]
    public void Function_WithNoReturnSchema_HasNone()
    {
        // the reason a return schema is nullable
        var nothing = AIFunctionFactory.Create(() => { }, "nothing", "Returns nothing.");

        Assert.Null(new AgentToolDescriptor(nothing, Declaration(name: "nothing")).Tool.ReturnJsonSchema);
    }

    [Fact]
    public async Task Invoke_BindsArgumentsAndReturnsTheResult()
    {
        var json = await Invoker(Echo(), Declaration()).InvokeCompleteAsync(
            "echo", """{"message":"hello"}""", Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Contains("hello", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_ArgumentsThatAreNotJson_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<AgentToolArgumentException>(async () => await Invoker(Echo(), Declaration())
            .InvokeCompleteAsync("echo", "{ not json", Probes.Context(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Invoke_PassesTheInvocationThroughToTheFunction()
    {
        // a function that needs more than its arguments finds the whole invocation in its own
        // context bag, which is where the AI abstractions already keep such things
        AgentToolContext? seen = null;

        var function = AIFunctionFactory.Create(
            (AIFunctionArguments arguments) =>
            {
                seen = arguments.GetAgentToolContext();

                return "ok";
            },
            "inspect",
            "Looks at its own invocation.");

        var context = Probes.Context();

        await Invoker(function, Declaration(name: "inspect")).InvokeCompleteAsync(
            "inspect", null, context, TestContext.Current.CancellationToken);

        Assert.Same(context, seen);
    }

    [Fact]
    public async Task Invoke_PassesTheScopeThroughToTheFunction()
    {
        IServiceProvider? seen = null;

        var function = AIFunctionFactory.Create(
            (AIFunctionArguments arguments) =>
            {
                seen = arguments.Services;

                return "ok";
            },
            "inspect",
            "Looks at its own invocation.");

        var context = Probes.Context();

        await Invoker(function, Declaration(name: "inspect")).InvokeCompleteAsync(
            "inspect", null, context, TestContext.Current.CancellationToken);

        Assert.Same(context.Services, seen);
    }

    /// <summary>An invoker over one foreign function.</summary>
    /// <param name="function">The function.</param>
    /// <param name="declaration">What is declared about it.</param>
    /// <returns>The invoker.</returns>
    private static AgentToolInvoker Invoker(AIFunction function, AgentToolDeclaration declaration) =>
        new(new AgentToolRegistry([new StaticAgentToolSource([new AgentToolDescriptor(function, declaration)])]));
}
