using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Searches the tickets.
/// </summary>
/// <remarks>
/// The tool with the richest arguments, which is where the generated input schema earns its keep:
/// every field's description, its optionality and the enum's allowed values come from the record.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsSearchTool(SupportDeskStore store) : SupportDeskTool<TicketSearchArguments, IReadOnlyList<TicketSummary>>
{
    /// <summary>The most a single search will report.</summary>
    private const int MaximumLimit = 50;

    /// <inheritdoc />
    public override string Name => "support_tickets_search";

    /// <inheritdoc />
    public override string Title => "Search tickets";

    /// <inheritdoc />
    public override string Description =>
        "Finds support tickets whose subject or body contains the given words, optionally narrowed to one " +
        "status. Prefer this over support_tickets_list when looking for something in particular, since it " +
        "returns far less to read.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<TicketSummary>> ExecuteAsync(TicketSearchArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (arguments.Limit is < 1 or > MaximumLimit)
        {
            // a validation failure reads as an argument to correct, which is what a model can act on
            throw new AgentToolArgumentException($"Limit has to be between 1 and {MaximumLimit}.");
        }

        var matches = store.Tickets()
            .Where(ticket => arguments.Status is null || ticket.Status == arguments.Status)
            .Where(ticket => string.IsNullOrWhiteSpace(arguments.Query)
                             || ticket.Subject.Contains(arguments.Query, StringComparison.OrdinalIgnoreCase)
                             || ticket.Body.Contains(arguments.Query, StringComparison.OrdinalIgnoreCase))
            .Take(arguments.Limit)
            .Select(ticket => new TicketSummary
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Status = ticket.Status,
                Assignee = ticket.Assignee
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<TicketSummary>>(matches);
    }
}
