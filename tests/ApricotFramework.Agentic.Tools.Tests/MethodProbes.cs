using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// Tools written as methods rather than as a class each.
/// </summary>
/// <param name="dependency">Something the container hands out once per scope.</param>
/// <remarks>
/// The class is resolved per invocation exactly as a class-per-tool is, so it takes what it needs
/// through its constructor - scoped dependencies included.
/// </remarks>
[AgentToolType]
public sealed class MethodProbes(ScopedDependency dependency)
{
    /// <summary>Reports which scoped dependency this call was given.</summary>
    /// <returns>The ordinal.</returns>
    [AgentTool("probe_methods_scope", Title = "Read the scope", ReadOnly = true)]
    [Description("Reports which scoped dependency this call was given.")]
    public int ReadScope() => dependency.Ordinal;

    /// <summary>Adds two numbers, needing nothing from the container to do it.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>The sum.</returns>
    /// <remarks>Static, because a tool that depends on nothing should not have to pretend to.</remarks>
    [AgentTool("probe_methods_add", ReadOnly = true)]
    [Description("Adds two numbers together.")]
    public static int Add([Description("The first number.")] int left, [Description("The second number.")] int right) => left + right;

    /// <summary>A method whose C# name and tool name have nothing to do with each other.</summary>
    /// <returns>Something.</returns>
    /// <remarks>
    /// Which is the ordinary case, and why the name is a constructor argument: a method name is
    /// a C# name and a tool name is part of a prompt.
    /// </remarks>
    [AgentTool("probe_methods_read", ReadOnly = true)]
    [Description("Reads the thing.")]
    public Task<string> ReadTheThingAsync() => Task.FromResult($"ok-{dependency.Ordinal}");

    /// <summary>Reports who is asking.</summary>
    /// <param name="context">The invocation, bound from the call rather than from the schema.</param>
    /// <param name="note">Something the caller does supply.</param>
    /// <returns>Who asked, and what they said.</returns>
    [AgentTool("probe_methods_caller", ReadOnly = true)]
    [Description("Reports who is asking.")]
    public static string Caller(AgentToolContext context, [Description("A note.")] string note) =>
        $"{context.User?.Identity?.Name ?? "nobody"}:{note}";

    /// <summary>A method that is not a tool.</summary>
    /// <returns>Nothing anyone can call.</returns>
    public static string NotATool() => "no";
}
