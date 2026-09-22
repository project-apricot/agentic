using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Raises a ticket.
/// </summary>
/// <remarks>
/// A write that is <em>not</em> idempotent: calling it twice raises two tickets. The library
/// defaults <see cref="AgentTool.IsIdempotent"/> to whatever read-only says, so a write like this
/// has to say so - and saying so is what lets a caller know a retry is not free.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.TicketsWrite)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Internal)]
public sealed class TicketsCreateTool(SupportDeskStore store) : SupportDeskTool<TicketCreateArguments, TicketChanged>
{
    /// <inheritdoc />
    public override string Name => "support_tickets_create";

    /// <inheritdoc />
    public override string Title => "Raise a ticket";

    /// <inheritdoc />
    public override string Description =>
        "Raises a new support ticket on behalf of a customer. Check with support_tickets_search first " +
        "that the customer has not already raised the same thing, because this creates a second ticket " +
        "rather than finding the first.";

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsIdempotent => false;

    /// <inheritdoc />
    protected override Task<TicketChanged> ExecuteAsync(TicketCreateArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (store.Customer(arguments.CustomerId) is null)
        {
            throw new AgentToolArgumentException($"No customer {arguments.CustomerId} exists.");
        }

        var ticket = store.Create(arguments.Subject, arguments.Body, arguments.CustomerId);

        return Task.FromResult(new TicketChanged { Id = ticket.Id, Outcome = "raised" });
    }
}
