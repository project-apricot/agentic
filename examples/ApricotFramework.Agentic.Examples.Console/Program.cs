using ApricotFramework.Agentic.Examples.Console;
using ApricotFramework.Agentic.Examples.Console.Tools;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Mcp.Client;
using ApricotFramework.Agentic.Tools.Mcp.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

// A host with no web framework in it. The claim the package split is for: a console or desktop
// application that wants a registry over its own tools and a few MCP servers takes two packages
// and nothing else.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<Notebook>();

// the machinery, and the decisions where sequence is part of the meaning: which MCP servers a
// caller reaches, and how their tools are offered here. this host has no notion of a person, so
// it takes the core's answer to who is asking - nobody
builder.Services.AddAgentToolsCore()
    .WithStaticMcpClients(builder.Configuration)
    .WithMcpTools(options => options.Prefix = "up_");

// additive wiring, and deliberately not part of that chain: one class, three tools, and it could
// as easily live in another file. it is resolved from the container once per call, so a scoped
// dependency in its constructor is a fresh one every time
builder.Services.AddAgentToolType<NotesTools>();

using var host = builder.Build();

await host.StartAsync();

var executor = host.Services.GetRequiredService<IAgentToolExecutor>();

var tools = await executor.GetAvailableToolsAsync();

Console.WriteLine($"{tools.Count} tools, from {tools.Select(Origin).Distinct().Count()} places\n");

foreach (var tool in tools.OrderBy(tool => tool.Name, StringComparer.Ordinal))
{
    Console.WriteLine($"  {tool.Name,-34} {Origin(tool),-12} {Behaviour(tool)}");
}

// two calls to the same tool, each in a scope of its own. the numbers differ, which is the
// property a captured scoped dependency would have hidden
Console.WriteLine("\nlocal, twice:");
Console.WriteLine("  " + await executor.InvokeCompleteAsync("notes_scope", null));
Console.WriteLine("  " + await executor.InvokeCompleteAsync("notes_scope", null));

Console.WriteLine("\nlocal, with arguments:");
Console.WriteLine("  " + Compact(await executor.InvokeCompleteAsync("notes_echo", """{"message":"hello"}""")));

// and a tool this process did not write, reached over stdio, invoked exactly the same way
var upstream = tools.FirstOrDefault(tool => tool.Name.EndsWith("_echo", StringComparison.Ordinal) && tool.Name.StartsWith("up_", StringComparison.Ordinal));

if (upstream is not null)
{
    Console.WriteLine($"\nupstream ({upstream.Name}):");
    Console.WriteLine("  " + Compact(await executor.InvokeCompleteAsync(upstream.Name, """{"message":"hello from apricot"}""")));
}

// the same tools, as functions a chat client would be handed. nothing adapts them: an AgentTool
// is an AIFunction, and so is an upstream server's tool
var functions = await executor.GetAvailableFunctionsAsync();

var options = new Microsoft.Extensions.AI.ChatOptions { Tools = [.. functions] };

Console.WriteLine($"\nChatOptions.Tools: {options.Tools!.Count} functions, ready for an IChatClient");

await host.StopAsync();

static string Origin(AgentToolDescriptor tool) =>
    tool.Declaration.Labels.TryGetValue(McpAgentToolSource.OriginLabel, out var origin) ? origin?.ToString() ?? "local" : "local";

static string Behaviour(AgentToolDescriptor tool) => tool.Declaration switch
{
    { IsDestructive: true } => "destructive",
    { IsReadOnly: true } => "read only",
    _ => "writes"
};

static string Compact(string json)
{
    using var document = JsonDocument.Parse(json);

    var compacted = JsonSerializer.Serialize(document.RootElement);

    return compacted.Length <= 160 ? compacted : compacted[..157] + "...";
}
