using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Looks a company up in a public registry outside the application.
/// </summary>
/// <remarks>
/// Open world, which is the flag saying this reaches something the application does not own. Worth
/// declaring because a caller reasons differently about it: the answer can change without anything
/// here changing, and it can fail for reasons no amount of correct arguments will fix.
/// </remarks>
[Authorize(Policy = SupportDeskPolicies.CustomersRead)]
[AgentToolLabel(SupportDeskLabels.Sensitivity, Sensitivity.Public)]
public sealed class CompanyRegistryLookupTool : SupportDeskTool<CompanyLookupArguments, CompanyRecord>
{
    /// <summary>What the pretend registry knows.</summary>
    private static readonly Dictionary<string, CompanyRecord> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Northwind"] = new CompanyRecord { Name = "Northwind Traders Ltd", Registration = "04785213", Country = "GB" },
        ["Contoso"] = new CompanyRecord { Name = "Contoso Corporation", Registration = "C-9917", Country = "US" }
    };

    /// <inheritdoc />
    public override string Name => "support_registry_lookup";

    /// <inheritdoc />
    public override string Title => "Look a company up";

    /// <inheritdoc />
    public override string Description =>
        "Looks a company up in the public companies registry and reports its registered name, number and " +
        "country. Reaches a service outside Enlight, so it can fail or return nothing for reasons unrelated " +
        "to the arguments. Use it to confirm a customer's company details, not to find customers.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override bool IsOpenWorld => true;

    /// <inheritdoc />
    protected override Task<CompanyRecord> ExecuteAsync(CompanyLookupArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        if (!Registry.TryGetValue(arguments.CompanyName, out var record))
        {
            throw new AgentToolNotFoundException($"The registry has no company called '{arguments.CompanyName}'.");
        }

        return Task.FromResult(record);
    }
}
