using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// What the desk can do with a ticket.
/// </summary>
/// <remarks>
/// <para>
/// One class, several tools. The class is resolved from the container once per invocation, so
/// <see cref="SupportDeskStore"/> arrives through the constructor exactly as it would in an
/// endpoint - and a scoped store would be a fresh one for every call.
/// </para>
/// <para>
/// The labels here cover the whole family; a method restating one by name overrides it for that
/// tool only, which is how the two destructive tools narrow their surface.
/// </para>
/// </remarks>
/// <param name="store">Where the tickets are.</param>
[AgentToolType]
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
[Sensitivity(Sensitivity.Internal)]
public sealed class TicketTools(SupportDeskStore store)
{
    /// <summary>The most a single search will report.</summary>
    private const int MaximumLimit = 50;

    /// <summary>The youngest a purged ticket may be.</summary>
    private const int MinimumAgeInDays = 30;

    /// <summary>Reads one ticket.</summary>
    /// <param name="id">Which ticket.</param>
    /// <returns>The ticket in full.</returns>
    [AgentTool("support_tickets_get", Title = "Get a ticket", ReadOnly = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsRead)]
    [Description("Reads one support ticket in full, including what the customer wrote, the notes the desk added " +
                 "and how it was resolved. Use this after a listing or a search has narrowed to one ticket and " +
                 "the summary does not carry the field you need.")]
    public TicketDetail Get(
        [Description("The identifier of the ticket, as reported by support_tickets_list or support_tickets_search.")] long id)
    {
        var ticket = store.Ticket(id) ?? throw new AgentToolNotFoundException($"No ticket {id} exists.");

        return new TicketDetail
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Body = ticket.Body,
            Status = ticket.Status,
            Assignee = ticket.Assignee,
            CustomerId = ticket.CustomerId,
            Resolution = ticket.Resolution,
            Notes = ticket.Notes,
            Raised = ticket.Raised
        };
    }

    /// <summary>Finds tickets.</summary>
    /// <param name="query">What to look for.</param>
    /// <param name="status">Which state to keep.</param>
    /// <param name="limit">How many to return.</param>
    /// <returns>The matches.</returns>
    [AgentTool("support_tickets_search", Title = "Search tickets", ReadOnly = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsRead)]
    [Description("Finds support tickets whose subject or body contains the given words, optionally narrowed to one " +
                 "status. Prefer this over support_tickets_list when looking for something in particular, since it " +
                 "returns far less to read.")]
    public IReadOnlyList<TicketSummary> Search(
        [Description("Words to look for in the subject or body. Leave unset to match every ticket.")] string? query = null,
        [Description("Keep only tickets in this state. Leave unset to match every state.")] TicketStatus? status = null,
        [Description("How many tickets to return, at most 50. Defaults to 10.")] int limit = 10)
    {
        if (limit is < 1 or > MaximumLimit)
        {
            // a validation failure reads as an argument to correct, which is what a model can act on
            throw new AgentToolArgumentException($"Limit has to be between 1 and {MaximumLimit}.");
        }

        return
        [
            .. store.Tickets()
                .Where(ticket => status is null || ticket.Status == status)
                .Where(ticket => string.IsNullOrWhiteSpace(query)
                                 || ticket.Subject.Contains(query, StringComparison.OrdinalIgnoreCase)
                                 || ticket.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .Select(Summarise)
        ];
    }

    /// <summary>Raises a ticket.</summary>
    /// <param name="subject">A one line summary.</param>
    /// <param name="body">The detail.</param>
    /// <param name="customerId">Who raised it.</param>
    /// <returns>What changed.</returns>
    [AgentTool("support_tickets_create", Title = "Raise a ticket", Idempotent = false)]
    [Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
    [Description("Raises a new support ticket on behalf of a customer. Check with support_tickets_search first " +
                 "that the customer has not already raised the same thing, because this creates a second ticket " +
                 "rather than finding the first.")]
    public TicketChanged Create(
        [Description("A one line summary of what the customer needs.")] string subject,
        [Description("What the customer wrote, in full.")] string body,
        [Description("The identifier of the customer raising it, as reported by support_customers_list.")] long customerId)
    {
        if (store.Customer(customerId) is null)
        {
            throw new AgentToolArgumentException($"No customer {customerId} exists.");
        }

        return new TicketChanged { Id = store.Create(subject, body, customerId).Id, Outcome = "raised" };
    }

    /// <summary>Reassigns a ticket.</summary>
    /// <param name="id">Which ticket.</param>
    /// <param name="assignee">Who to assign it to.</param>
    /// <returns>What changed.</returns>
    [AgentTool("support_tickets_reassign", Title = "Reassign a ticket", Idempotent = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
    [Description("Assigns a support ticket to an agent, or clears the assignment when given no agent. Does not " +
                 "notify anyone; use support_notes_add to leave a note explaining why it moved.")]
    public TicketChanged Reassign(
        [Description("The identifier of the ticket to reassign.")] long id,
        [Description("The agent to assign it to. Pass null to leave it unassigned.")] string? assignee = null)
    {
        var ticket = store.Ticket(id) ?? throw new AgentToolNotFoundException($"No ticket {id} exists.");

        store.Replace(ticket with { Assignee = assignee });

        return new TicketChanged { Id = ticket.Id, Outcome = assignee is null ? "unassigned" : $"assigned to {assignee}" };
    }

    /// <summary>Closes a ticket.</summary>
    /// <param name="id">Which ticket.</param>
    /// <param name="resolution">How it was resolved.</param>
    /// <returns>What changed.</returns>
    [AgentTool("support_tickets_close", Title = "Close a ticket", Idempotent = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
    [Description("Closes a support ticket and records how it was resolved. The ticket and its notes are kept and " +
                 "stay readable; this is not a deletion.")]
    public TicketChanged Close(
        [Description("The identifier of the ticket to close.")] long id,
        [Description("How the ticket was resolved, for whoever reads it later.")] string resolution)
    {
        var ticket = store.Ticket(id) ?? throw new AgentToolNotFoundException($"No ticket {id} exists.");

        store.Replace(ticket with { Status = TicketStatus.Closed, Resolution = resolution });

        return new TicketChanged { Id = ticket.Id, Outcome = "closed" };
    }

    /// <summary>Adds a note.</summary>
    /// <param name="ticketId">Which ticket.</param>
    /// <param name="note">The note.</param>
    /// <returns>What changed.</returns>
    [AgentTool("support_notes_add", Title = "Add a note", Idempotent = false)]
    [Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
    [Description("Adds an internal note to a support ticket, visible to the desk but not to the customer. Use this " +
                 "to record what was tried or why a ticket moved. Notes cannot be edited or removed afterwards.")]
    public TicketChanged AddNote(
        [Description("The identifier of the ticket to add the note to.")] long ticketId,
        [Description("The note to add. Visible to the desk, not to the customer.")] string note)
    {
        var ticket = store.Ticket(ticketId) ?? throw new AgentToolNotFoundException($"No ticket {ticketId} exists.");

        store.Replace(ticket with { Notes = [.. ticket.Notes, note] });

        return new TicketChanged { Id = ticket.Id, Outcome = "note added" };
    }

    /// <summary>Deletes a ticket.</summary>
    /// <param name="id">Which ticket.</param>
    /// <returns>What changed.</returns>
    /// <remarks>Narrows the family's surface label: destructive, so not offered over MCP.</remarks>
    [AgentTool("support_tickets_delete", Title = "Delete a ticket", Destructive = true, Idempotent = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
    [AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Internal)]
    [Description("Permanently deletes a support ticket and every note on it. There is no undo and no record kept. " +
                 "Prefer support_tickets_close, which keeps the ticket readable; delete only what should never " +
                 "have been recorded.")]
    public TicketChanged Delete(
        [Description("The identifier of the ticket to delete.")] long id)
    {
        if (!store.Delete(id))
        {
            throw new AgentToolNotFoundException($"No ticket {id} exists.");
        }

        return new TicketChanged { Id = id, Outcome = "deleted" };
    }

    /// <summary>Purges old closed tickets.</summary>
    /// <param name="olderThanDays">How old they have to be.</param>
    /// <returns>How many went.</returns>
    [AgentTool("support_tickets_purge", Title = "Purge closed tickets", Destructive = true, Idempotent = false)]
    [Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
    [AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Internal)]
    [Description("Permanently deletes every closed support ticket older than the given number of days. Affects " +
                 "many tickets at once, cannot be undone, and keeps no record of what went. Read the count with " +
                 "support_tickets_search before calling this.")]
    public TicketsPurged Purge(
        [Description("Purge closed tickets older than this many days. Must be at least 30.")] int olderThanDays)
    {
        if (olderThanDays < MinimumAgeInDays)
        {
            throw new AgentToolArgumentException($"Tickets have to be at least {MinimumAgeInDays} days old to be purged.");
        }

        return new TicketsPurged { Deleted = store.PurgeClosedBefore(DateTime.UtcNow.AddDays(-olderThanDays)) };
    }

    /// <summary>Exports everything, and is deliberately not offered.</summary>
    /// <returns>Every ticket.</returns>
    /// <remarks>
    /// Carries <see cref="AgentToolIgnoreAttribute"/>, so discovery passes over it. A tool that is a tool
    /// in every respect except that this host should not offer it - half-written, kept for a test, or
    /// belonging to a surface this deployment does not serve.
    /// </remarks>
    [AgentToolIgnore]
    [AgentTool("support_tickets_export", Title = "Export tickets", ReadOnly = true)]
    [Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
    [Description("Exports every ticket on the desk.")]
    public IReadOnlyList<TicketSummary> Export() => [.. store.Tickets().Select(Summarise)];

    /// <summary>Describes a ticket the way a listing does.</summary>
    /// <param name="ticket">The ticket.</param>
    /// <returns>The summary.</returns>
    internal static TicketSummary Summarise(Ticket ticket) => new()
    {
        Id = ticket.Id,
        Subject = ticket.Subject,
        Status = ticket.Status,
        Assignee = ticket.Assignee
    };
}
