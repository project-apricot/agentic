# ApricotFramework.Agentic

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Agentic.Tools.svg?label=ApricotFramework.Agentic.Tools)](https://www.nuget.org/packages/ApricotFramework.Agentic.Tools/)
[![CI](https://github.com/project-apricot/agentic/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/agentic/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/agentic/blob/main/LICENSE)

Declare an AI tool once, beside the code that runs it, and surface it anywhere. A tool carries its
own name, the prompt a model selects on, JSON schemas generated from ordinary .NET types, and the
behaviour a caller's policy depends on.

**A tool is a `Microsoft.Extensions.AI.AIFunction`**, not something adapted into one — so the same
declaration goes into `ChatOptions.Tools` for an in-process loop, through `McpServerTool.Create`
for an MCP server, and over gRPC to another service, with no adapter in any direction. Tools
arriving from those same directions come back the other way with none either.

**Nothing here knows how a tool is reached.** Local or remote is a property of the caller, not of
the operation.

**Nothing here knows what a permission is.** What a caller must hold is declared the way the host
already declares it — an attribute — and decided by the host's own pipeline.

> **Status: preview.** The shape is still moving. Expect breaking changes until 1.0.

## Install

Nothing implies anything else. Take the surfaces this host actually has.

```bash
dotnet add package ApricotFramework.Agentic.Tools              # registry, executor, registration
dotnet add package ApricotFramework.Agentic.Tools.Abstractions # a library that only declares tools
dotnet add package ApricotFramework.Agentic.Tools.AspNetCore   # authorization, and a caller from the request
dotnet add package ApricotFramework.Agentic.Tools.Mcp.Client   # tools from upstream MCP servers
dotnet add package ApricotFramework.Agentic.Tools.Mcp.Server   # this host's tools, over MCP
dotnet add package ApricotFramework.Agentic.Tools.Grpc.Server  # this host's tools, over gRPC
dotnet add package ApricotFramework.Agentic.Tools.Grpc.Client  # tools from another service, over gRPC
```

## A tool

A tool is an ordinary method on an ordinary class.

```csharp
[AgentToolType]
[Authorize(Policy = "tickets.read")]        // your policy, your pipeline
public sealed class TicketTools(TicketStore tickets)   // scoped, and resolved per call
{
    [AgentTool("support_tickets_get", Title = "Get a ticket", ReadOnly = true)]
    [Description("Reads one ticket in full, given its identifier. Use this after " +
                 "support_tickets_list has narrowed to a particular ticket.")]
    public async Task<TicketDetail> Get(
        [Description("The identifier of the ticket.")] long id,
        CancellationToken cancellationToken)
        => Map(await tickets.GetById(id, cancellationToken));
}
```

The parameter list is the argument shape and the schemas come from the types. `[Description]`
becomes what a model reads; nullability becomes the difference between required and optional. A
parameter typed `AgentToolContext`, `CancellationToken` or `IProgress<T>` is bound from the call
and left out of the schema.

Binding and schema generation are `AIFunctionFactory`'s — the same machinery `[McpServerTool]`
uses — which is why there is no class-per-tool base to derive. The one case that needs a class is
a result that arrives as a sequence, because an `AIFunction` returns one value: derive `AgentTool`
and implement `InvokeStreamingAsync`. A consumer that cannot stream collects the items and sees
the same result.

## Registration

```csharp
builder.Services.AddAgentToolsWeb()                // registry, executor, tripwire, and the
                                                   // caller taken from the request
    .WithAuthorization();                          // gate tools on the attributes they declare

builder.Services.AddAgentToolType<TicketTools>();
builder.Services.AddAgentTool<TicketsListTool>();  // the hand-written one, for a sequence
builder.Services.AddAgentToolValidator<ToolNamingConvention>();   // your rules, checked at startup
```

Anything else to say about a tool is said inside the call, so nothing is handed back to be mutated
later:

```csharp
builder.Services.AddAgentToolType<TicketAdminTools>(tool => tool.RequireAuthorization("tickets.admin"));
```

## Invoking

There is no context parameter. Opening a scope and deciding who the caller is are one question a
host answers once, in an `IAgentToolContextFactory` — not something every endpoint, MCP handler
and gRPC method re-derives.

```csharp
var offered = await executor.GetAvailableToolsAsync(cancellationToken);
var json    = await executor.InvokeCompleteAsync(name, argumentsJson, cancellationToken);

await foreach (var chunk in executor.InvokeAsync(name, argumentsJson, cancellationToken)) { … }
```

A scope is opened per call and lives until the last item has been yielded, so a tool takes its
dependencies through its constructor the way an endpoint does.

For a chat client:

```csharp
var options = new ChatOptions { Tools = [.. await executor.GetAvailableFunctionsAsync(cancellationToken)] };
```

Each of those re-enters the executor when called, so a conversation that listed twenty minutes ago
still runs in a fresh scope — and is filtered again, because a listing is not a permission.

## Tools from somewhere else

```csharp
builder.Services.AddAgentToolsCore()
    .WithStaticMcpClients(builder.Configuration)               // servers named in configuration
    .WithMcpTools(options =>
{
    options.Prefix = "up_";                                    // a name space of your own
    options.Metadata.Add(new AuthorizeAttribute("tools.external"));
});
```

An MCP client's tool is already an `AIFunction`, so nothing wraps it: what this host claims about
it — a name, a pinned description, a gate — sits beside it in the descriptor, and a consumer
reaching through it for the real thing still finds it. The registry asks on every listing rather
than once at start-up, so a server that adds a tool is reflected without a restart, and a host
that connects servers per person gets a surface per person.

The same shape over gRPC brings in another service's tools, point to point.

## Design notes

**The registry insists on almost nothing.** A tool needs a name, and no two may share one. A
title, a description, who may call a tool and which surface it belongs on are validators and
filters you add, because a library that insists on its own judgement is one you eventually work
around.

**A tool that declares authorization nothing enforces stops the host.** That is the tripwire, and
the reason there is no setting to turn authorization on.

**One decision covers the listing and the call.** A listing exists to avoid offering what the call
would refuse, so a filter applies to both.

**Results are JSON text.** Not a format that carries numbers as doubles — an `Int64` identifier
past 2^53 has to survive the round trip.

Full documentation: <https://projectapricot.dev/docs/agentic>
