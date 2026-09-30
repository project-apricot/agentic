using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// What the desk can do with a customer.
/// </summary>
/// <param name="store">Where the customers are.</param>
[AgentToolType]
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
[Sensitivity(Sensitivity.Personal)]
public sealed class CustomerTools(SupportDeskStore store)
{
    /// <summary>Lists the customers.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Each customer in turn.</returns>
    /// <remarks>
    /// Returns <see cref="IAsyncEnumerable{T}"/>, which the function factory understands: the schema
    /// describes the assembled array and the items are collected before the result is returned. A tool
    /// whose caller should see items as they arrive has to be an <see cref="AgentTool"/> - see
    /// <see cref="TicketsListTool"/>.
    /// </remarks>
    [AgentTool("support_customers_list", Title = "List customers", ReadOnly = true)]
    [Authorize(Policy = SupportDeskPolicies.CustomersRead)]
    [Description("Lists the customers the desk knows about, with their names and companies. Use this to find a " +
                 "customer's identifier before raising a ticket for them. Returns no contact details; use " +
                 "support_customers_get for those.")]
    public async IAsyncEnumerable<CustomerSummary> List([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var customer in store.Customers())
        {
            await Task.Yield();

            yield return new CustomerSummary { Id = customer.Id, Name = customer.Name, Company = customer.Company };
        }
    }

    /// <summary>Reads one customer.</summary>
    /// <param name="id">Which customer.</param>
    /// <returns>The customer in full.</returns>
    [AgentTool("support_customers_get", Title = "Get a customer", ReadOnly = true)]
    [Authorize(Policy = SupportDeskPolicies.CustomersRead)]
    [Description("Reads one customer in full, including the email address they contact the desk from and how many " +
                 "tickets they have raised. Use this when the listing does not carry what you need.")]
    public CustomerDetail Get(
        [Description("The identifier of the customer, as reported by support_customers_list.")] long id)
    {
        var customer = store.Customer(id) ?? throw new AgentToolNotFoundException($"No customer {id} exists.");

        return new CustomerDetail
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Company = customer.Company,
            TicketCount = store.TicketCount(customer.Id)
        };
    }
}
