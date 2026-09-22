using System.Runtime.CompilerServices;
using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Lists the customers.
/// </summary>
/// <remarks>
/// Labelled <see cref="Sensitivity.Personal"/>, which is the label doing real work: a policy that
/// refuses personal data to a caller without the right claim can find this without knowing what a
/// customer is.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.CustomersRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Personal)]
public sealed class CustomersListTool(SupportDeskStore store) : SupportDeskStreamTool<AgentToolNoArguments, CustomerSummary>
{
    /// <inheritdoc />
    public override string Name => "support_customers_list";

    /// <inheritdoc />
    public override string Title => "List customers";

    /// <inheritdoc />
    public override string Description =>
        "Lists the customers the desk knows about, with their names and companies. Use this to find a " +
        "customer's identifier before raising a ticket for them. Returns no contact details; use " +
        "support_customers_get for those.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    protected override async IAsyncEnumerable<CustomerSummary> ExecuteAsync(
        AgentToolNoArguments arguments,
        AgentToolContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var customer in store.Customers())
        {
            await Task.Yield();

            yield return new CustomerSummary { Id = customer.Id, Name = customer.Name, Company = customer.Company };
        }
    }
}
