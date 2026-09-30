using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// What the billing service offers.
/// </summary>
[AgentToolType]
public sealed class BillingTools
{
    /// <summary>Reads an invoice.</summary>
    /// <param name="id">The invoice.</param>
    /// <returns>Which invoice, from which service.</returns>
    [AgentTool("invoices_get", ReadOnly = true)]
    [Description("Reads an invoice.")]
    public static string Get(long id) => $"billing:{id}";

    /// <summary>Fails the way a bug fails, which is a fault and not a refusal.</summary>
    /// <returns>Nothing it ever gets to return.</returns>
    /// <exception cref="InvalidOperationException">Always.</exception>
    [AgentTool("invoices_fail", ReadOnly = true)]
    [Description("Always fails.")]
    public static string Fail() => throw new InvalidOperationException("The ledger is unreachable.");
}
