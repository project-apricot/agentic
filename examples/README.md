# Examples

Three projects. The first two are the split that is the point — a library declaring tools, and a
host surfacing them. The third is the claim that the host need not be a web application at all.

| Project | What it is |
| --- | --- |
| `ApricotFramework.Agentic.Examples.SupportDesk` | Fifteen tools, the rules they are checked against, and one place wiring them up |
| `ApricotFramework.Agentic.Examples.Web` | Endpoints to list and invoke them, with a `.http` file to drive it |
| `ApricotFramework.Agentic.Examples.Console` | No web framework at all: local tools beside tools federated from a real MCP server |

```bash
dotnet run --project examples/ApricotFramework.Agentic.Examples.Web
```

Then open `ApricotFramework.Agentic.Examples.Web.http`. Authentication is faked from headers, so
there is no token issuer to run: send `X-Demo-Subject` and a space separated `X-Demo-Scopes`.

```bash
dotnet run --project examples/ApricotFramework.Agentic.Examples.Console
```

That one needs `npx` on the path, because it launches
`@modelcontextprotocol/server-everything` over stdio and lists whatever it offers beside its own
three tools.

## What the tools between them demonstrate

| Tool | Notable for |
| --- | --- |
| `support_tickets_list` | The one hand-written `AgentTool` — a sequence, items as they arrive |
| `support_tickets_get` | One result, one argument |
| `support_tickets_search` | Rich arguments: optional fields, an enum, a validated bound |
| `support_tickets_create` | A write that is **not** idempotent, and says so |
| `support_tickets_reassign` | A write that **is** idempotent |
| `support_tickets_close` | A write that keeps the record, unlike a delete |
| `support_notes_add` | Append-only, so not idempotent |
| `support_tickets_delete` | **Destructive**, and internal-surface only |
| `support_tickets_purge` | Destructive, bulk, and not idempotent — the worst combination |
| `support_customers_list` | Labelled `sensitivity=personal` |
| `support_customers_get` | Personal data in full |
| `support_registry_lookup` | **Open world** — reaches something the application does not own |
| `support_status_check` | Read-only, open world, and **not idempotent** — same call, different answer |
| `support_customers_list` | A method returning `IAsyncEnumerable<T>` — works, and collects |
| `support_text_summarise` | No class and no method: a delegate registered as an `AIFunction` with a declaration beside it |
| `support_kb_*` | Tools from a source the application does not control |
| `support_tickets_export` | `[AgentToolIgnore]` on a method — one tool held back without moving it out of its holder |

## And what the wiring demonstrates

- **Three method holders** (`TicketTools`, `CustomerTools`, `OutsideTools`), each carrying the
  labels and the authorization its family declares, which individual methods narrow — the two
  destructive tools restate `surfaces` to keep themselves off the MCP surface.
- **One hand-written `AgentTool`** (`TicketsListTool`), because its result arrives as a sequence
  and an `AIFunction` returns one value. What that costs in boilerplate is visible, and is the
  reason the other fourteen are methods.
- **A typed label** (`SensitivityAttribute`, deriving `AgentToolLabelAttribute`) so the
  vocabulary is closed at the point of declaration — no label name to mistype, no value outside
  the enum. Nothing that reads it knows the type exists: the validator below and `/tools` both
  still see an ordinary `sensitivity` label.
- **Two validators of the application's own**: a naming convention, and one closing the
  `Sensitivity` label vocabulary so a tool cannot ship without stating one.
- **Authorization by attribute** on tools that have a class, and by `RequireAuthorization` at
  registration on the ones that do not.
- **Curation** of a foreign source, so one malformed third-party declaration costs that tool
  rather than the whole host. Watch the log on the first listing: `support_kb_retire` describes
  itself with whitespace and is left out with a warning. Note that the foreign functions are
  **not wrapped** — they go into their descriptors as they arrived, with what this application
  claims about them beside them.
- **A wrapper** (`AuditingAgentToolInvoker`) recording every call, registered with
  `DecorateInvoker` — the seam that sees the caller.
- **A context factory** (`SupportDeskAgentToolContextFactory`, in the web project) answering who
  is asking once, so no endpoint builds a context of its own. The `?surface=` query string is
  this application's own idea, and this is where it is read.
- **Startup validation**, registered by `AddAgentToolsCore()`, so a malformed declaration of the
  application's own stops the host rather than surfacing to whoever asks first.

## Two things worth trying

**`GET /tools` with no headers at all.** It comes back empty rather than refused — a caller is
never offered what it cannot invoke. Then add scopes and watch the list grow. Empty because every
tool here is gated: a tool declaring nothing would be listed, the way an endpoint with no
`[Authorize]` is reachable.

**`GET /tools?surface=mcp` as an admin.** The two destructive tools vanish from the listing,
because they declare themselves internal-only — and invoking one on that surface is a `404`, as
for a name nobody declared: off that surface the tool does not exist, so there is nothing to
authorize. The same filter answers both questions, which is why the listing and the gate cannot
disagree.

## And in the console example

**Two calls to `notes_scope` report two different scopes.** That is the property the whole hosting
layer exists for: a tool is built again for every call, from the caller's own scope, so a scoped
dependency in its constructor works the way it does in an endpoint.

**`up_everything_echo` is invoked exactly like `notes_echo`.** One is a method in this process and
one is a process launched over stdio, and the call site cannot tell.

**The last line prints `ChatOptions.Tools`.** Local and upstream tools alike go into a chat
client's options with nothing adapting them, because both are already `AIFunction`.
