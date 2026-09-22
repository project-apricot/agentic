# Examples

Two projects, because the split is the point: a library declaring tools, and a host surfacing them.

| Project | What it is |
| --- | --- |
| `ApricotFramework.Agentic.Examples.SupportDesk` | Fifteen tools, the rules they are checked against, and one place wiring them up |
| `ApricotFramework.Agentic.Examples.Web` | Endpoints to list and invoke them, with a `.http` file to drive it |

```bash
dotnet run --project examples/ApricotFramework.Agentic.Examples.Web
```

Then open `ApricotFramework.Agentic.Examples.Web.http`. Authentication is faked from headers, so
there is no token issuer to run: send `X-Demo-Subject` and a space separated `X-Demo-Scopes`.

## What the tools between them demonstrate

| Tool | Notable for |
| --- | --- |
| `support_tickets_list` | A sequence tool — items as they arrive |
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
| `support_text_summarise` | No class at all: a delegate adapted from an `AIFunction` |
| `support_kb_*` | Tools from a source the application does not control |
| `support_tickets_export` | `[AgentToolIgnore]` — a tool deliberately not offered |

## And what the wiring demonstrates

- **A shared base class** (`SupportDeskTool<,>`) carrying the labels a whole family declares,
  which derived tools narrow.
- **Two validators of the application's own**: a naming convention, and one closing the
  `Sensitivity` label vocabulary so a tool cannot ship without stating one.
- **Authorization by attribute** on tools that have a class, and by `RequireAuthorization` at
  registration on the ones that do not.
- **Curation** of a foreign source, so one malformed third-party declaration costs that tool
  rather than the whole host. Watch the log on startup: `support_kb_retire` describes itself with
  whitespace and is left out with a warning.
- **A wrapper** (`AuditingAgentToolInvoker`) recording every call, registered with
  `DecorateAgentToolInvoker`.
- **Composing the registry during startup**, so a malformed declaration stops the host rather than
  surfacing to whoever asks first.

## Two things worth trying

**`GET /tools` with no headers at all.** It comes back empty rather than refused — a caller is
never offered what it cannot invoke. Then add scopes and watch the list grow. Empty because every
tool here is gated: a tool declaring nothing would be listed, the way an endpoint with no
`[Authorize]` is reachable.

**`GET /tools?surface=mcp` as an admin.** The two destructive tools vanish, because they declare
themselves internal-only. Invoking one on that surface reports it as **absent** rather than
forbidden: from there it never existed, and telling a model a tool exists but is barred invites it
to keep asking.
