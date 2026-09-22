using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Deletes closed tickets past a cutoff.
/// </summary>
/// <remarks>
/// Destructive, not idempotent, and internal only - the worst combination the model can describe,
/// which is exactly why the description spells out what it cannot be undone from. A filter reading
/// the sensitivity label and the destructive flag would refuse this to most callers before the
/// policy ever ran.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsAdmin)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Internal)]
public sealed class TicketsPurgeTool(SupportDeskStore store) : SupportDeskTool<TicketPurgeArguments, TicketsPurged>
{
    /// <summary>The youngest a purged ticket may be.</summary>
    private const int MinimumAgeInDays = 30;

    /// <inheritdoc />
    public override string Name => "support_tickets_purge";

    /// <inheritdoc />
    public override string Title => "Purge closed tickets";

    /// <inheritdoc />
    public override string Description =>
        "Permanently deletes every closed support ticket older than the given number of days. Affects " +
        "many tickets at once, cannot be undone, and keeps no record of what went. Read the count with " +
        "support_tickets_search before calling this.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => true;

    /// <inheritdoc />
    public override bool IsIdempotent => false;


    /// <inheritdoc />
    protected override Task<TicketsPurged> ExecuteAsync(TicketPurgeArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (arguments.OlderThanDays < MinimumAgeInDays)
        {
            throw new AgentToolArgumentException($"Tickets have to be at least {MinimumAgeInDays} days old to be purged.");
        }

        var deleted = store.PurgeClosedBefore(DateTime.UtcNow.AddDays(-arguments.OlderThanDays));

        return Task.FromResult(new TicketsPurged { Deleted = deleted });
    }
}
