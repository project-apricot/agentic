# ApricotFramework.Agentic

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Agentic.Tools.svg?label=ApricotFramework.Agentic.Tools)](https://www.nuget.org/packages/ApricotFramework.Agentic.Tools/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Agentic.Tools.AspNetCore.svg?label=ApricotFramework.Agentic.Tools.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.Agentic.Tools.AspNetCore/)
[![CI](https://github.com/project-apricot/agentic/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/agentic/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/agentic/blob/main/LICENSE)

Declare an AI tool once, beside the code that runs it, and surface it anywhere. A tool carries its
own name, the prompt a model selects on, JSON schemas generated from ordinary .NET types, and the
behaviour a caller's policy depends on.

**Nothing here knows how a tool is reached.** Local or remote is a property of the caller, not of
the operation, so the same declaration serves an in-process agent loop and a remote protocol such
as MCP.

**Nothing here knows what a permission is.** What a caller must hold is declared the way the host
already declares it — an attribute — and decided by the host's own pipeline.

> **Status: preview.** The shape is still moving. Expect breaking changes until 1.0.

## Install

```bash
dotnet add package ApricotFramework.Agentic.Tools
dotnet add package ApricotFramework.Agentic.Tools.AspNetCore
```

## A tool

```csharp
[AuthorizeAnyAccess(ContentAccesses.AuthorsRead)]        // your attribute, your pipeline
public class AuthorsGetTool(AuthorService authors) : AgentTool<AuthorGetArguments, AuthorDetail>
{
    public override string Name => "content_authors_get";
    public override string Title => "Get an author";

    public override string Description =>
        "Reads one author in full, given the author's identifier. Use this after " +
        "content_authors_list has narrowed to a particular author.";

    public override bool IsReadOnly => true;
    public override bool IsDestructive => false;

    protected override async Task<AuthorDetail> ExecuteAsync(
        AuthorGetArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
        => Map(await authors.GetById(arguments.Id));
}
```

Gating works with anything ASP.NET Core understands — a custom requirement attribute as above,
`[Authorize(Policy = "…")]`, `[Authorize(Roles = "…")]`, or several together. All of them are
resolved the way the authorization middleware resolves them for an endpoint.

The schemas come from `AuthorGetArguments` and `AuthorDetail`. `[Description]` on a property
becomes what a model reads; nullability becomes the difference between required and optional.

A tool whose caller can act on the first results before the last ones exist derives from
`AgentStreamTool<TArguments, TItem>` and returns `IAsyncEnumerable<TItem>` instead. A consumer that
cannot stream collects the items and sees the same result, so the choice costs it nothing.

## Registration

```csharp
builder.Services.AddAgentTools();               // registry, invoker, tripwire
builder.Services.AddAgentToolAuthorization();   // gate tools on the attributes they declare
builder.Services.AddAgentTool<AuthorsGetTool>();
builder.Services.AddAgentToolValidator<ToolNamingConvention>();   // your rules, checked at startup
```

or, for a host with enough tools that the list becomes its own problem:

```csharp
builder.Services.AddAgentToolsFromAssemblyContaining<AuthorsGetTool>();
```

Found by deriving from `AgentTool`, not by a marker attribute. Abstract bases, open generics and
anything marked `[AgentToolIgnore]` are skipped. A curated list is still better where you can have
one — scanning is how a tool surface grows without anyone deciding that it should.

## Tools that are not classes

A delegate, something built from configuration, or a tool read off a foreign MCP server — all of
them are `AIFunction`, and all of them adapt:

```csharp
builder.Services
    .AddAgentTool(function, new AgentToolCreateOptions
    {
        IsReadOnly    = true,
        IsDestructive = false,
    })
    .RequireAuthorization("tools.use");     // no type, so no attribute — say it here instead
```

`RequireAuthorization` **adds to** whatever the tool's type declares rather than replacing it, the
way the same two mechanisms compose on a minimal API endpoint.

## Invoking

The same component answers what a caller *could* do and then does it, so a listing and a gate
cannot disagree:

```csharp
var offered = await invoker.GetAvailableToolsAsync(context, cancellationToken);
```

```csharp
var context = new AgentToolContext
{
    User     = user,                      // null where the host has no notion of one
    Services = services,                  // the portable way to reach host state
};

// streaming, one chunk per item
await foreach (var chunk in invoker.InvokeAsync(name, argumentsJson, context, cancellationToken)) { … }

// or whole, for a caller that cannot stream
var json = await invoker.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
```

The tool and its arguments are parameters; the context is *who is asking*. A loop putting five
questions to an agent on somebody's behalf builds the context once and passes it to each call.

`AgentToolContext` is the extension point for whatever else a call needs to carry. It is not
sealed, so a host with state worth naming derives from it — one host's calls arrive on an HTTP
request and another's on a timer with no request in sight, and a tool written against the base
type works in both.

Authorization runs once, in the invoker, before the first item. A check each tool is trusted to
perform is a check a tool can forget, and the one that forgets looks exactly like the ones that do
not.

`IAgentToolInvoker` is an interface so a host can wrap it — an audit record, a span, a rate limit.
Derive from `DelegatingAgentToolInvoker` and override only what you change:

```csharp
builder.Services.DecorateAgentToolInvoker<AuditingInvoker>();
```

## Design notes

**The registry insists on almost nothing.** A tool needs a name, and no two may share one —
without that it cannot address them. Everything beyond that is a registration rather than a
setting, and there are no flags: a title, a description, who may call a tool and which surface it
belongs on are validators and filters you add, because a library that insists on its own judgement
is one you eventually work around.

**Nothing is registered you did not ask for.** `AddAgentTools()` brings a registry, an invoker and
a tripwire. Authorization is `AddAgentToolAuthorization()`, and a host with tools and no policies
configured gets an answer rather than an unresolvable `IAuthorizationService`.

**A tool that declares authorization nothing enforces stops the host.** That is the tripwire, and
the reason there is no setting to turn authorization on — forgetting the call would otherwise open
every tool that thought it was gated. A tool that declares nothing is listed and callable, as it is
under the MCP SDK, so a public tool can sit beside gated ones.

**One decision covers the listing and the call.** A listing exists to avoid offering what the call
would refuse, so a filter applies to both. What a filter deliberately cannot say is "listed, but
not callable right now" — a spent budget or a pending approval is about the call, and belongs in an
invoker wrapper.

**Results are JSON text.** Not a format that carries numbers as doubles — an `Int64` identifier
past 2^53 has to survive the round trip intact.

Full documentation: <https://projectapricot.dev/docs/agentic>
