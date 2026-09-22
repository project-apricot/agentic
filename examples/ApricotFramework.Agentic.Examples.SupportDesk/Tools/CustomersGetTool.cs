using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Reads one customer, contact details included.
/// </summary>
[Authorize(Policy = SupportDeskPolicies.CustomersRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Personal)]
public sealed class CustomersGetTool(SupportDeskStore store) : SupportDeskTool<CustomerReference, CustomerDetail>
{
    /// <inheritdoc />
    public override string Name => "support_customers_get";

    /// <inheritdoc />
    public override string Title => "Get a customer";

    /// <inheritdoc />
    public override string Description =>
        "Reads one customer in full, including the email address they contact the desk from and how many " +
        "tickets they have raised. Use this when the listing does not carry what you need.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override Task<CustomerDetail> ExecuteAsync(CustomerReference arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        var customer = store.Customer(arguments.Id)
                       ?? throw new AgentToolNotFoundException($"No customer {arguments.Id} exists.");

        return Task.FromResult(new CustomerDetail
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Company = customer.Company,
            TicketCount = store.TicketCount(customer.Id)
        });
    }
}
