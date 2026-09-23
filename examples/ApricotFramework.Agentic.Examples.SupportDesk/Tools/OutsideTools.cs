using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// The two tools that reach something the application does not own.
/// </summary>
/// <remarks>
/// Both are open world, and neither needs anything from the container - so both are static, which
/// is a shape the function factory supports and which says plainly that there is no state here.
/// </remarks>
[AgentToolType]
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
[Sensitivity(Sensitivity.Public)]
public sealed class OutsideTools
{
    /// <summary>What the pretend registry knows.</summary>
    private static readonly Dictionary<string, CompanyRecord> Registry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Northwind"] = new CompanyRecord { Name = "Northwind Traders Ltd", Registration = "04785213", Country = "GB" },
        ["Contoso"] = new CompanyRecord { Name = "Contoso Corporation", Registration = "C-9917", Country = "US" }
    };

    /// <summary>Looks a company up.</summary>
    /// <param name="companyName">Which company.</param>
    /// <returns>What the registry knows.</returns>
    [AgentTool("support_registry_lookup", Title = "Look a company up", ReadOnly = true, OpenWorld = true)]
    [Authorize(Policy = SupportDeskPolicies.CustomersRead)]
    [Description("Looks a company up in the public companies registry and reports its registered name, number and " +
                 "country. Reaches a service this application does not own, so it can fail or return nothing for reasons unrelated " +
                 "to the arguments. Use it to confirm a customer's company details, not to find customers.")]
    public static CompanyRecord LookUpCompany(
        [Description("The company name to look up in the public registry.")] string companyName) =>
        Registry.TryGetValue(companyName, out var record)
            ? record
            : throw new AgentToolNotFoundException($"The registry has no company called '{companyName}'.");

    /// <summary>Checks whether the desk's dependencies are up.</summary>
    /// <returns>What is reachable right now.</returns>
    /// <remarks>Read only, open world, and <em>not</em> idempotent: the same call gives a different answer.</remarks>
    [AgentTool("support_status_check", Title = "Check service status", ReadOnly = true, OpenWorld = true, Idempotent = false)]
    [Authorize]
    [Description("Checks whether the support desk's dependencies are reachable right now. The answer is only true " +
                 "as of the moment it is asked, so ask again rather than reusing an earlier answer.")]
    public static ServiceStatus CheckStatus()
    {
        // pretends to check something, and pretends it is flaky, so the example has one tool whose
        // answer moves
        var degraded = DateTime.UtcNow.Second % 10 == 0 ? new[] { "mail-relay" } : [];

        return new ServiceStatus { Healthy = degraded.Length == 0, Degraded = degraded, Checked = DateTime.UtcNow };
    }
}
