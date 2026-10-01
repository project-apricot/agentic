using System.ComponentModel;
using Grpc.Core;

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

    [AgentTool("invoices_missing", ReadOnly = true)]
    [Description("Reads an invoice that is not there.")]
    public static string Missing(long id) => throw new KeyNotFoundException($"No invoice {id}.");

    [AgentTool("invoices_busy", ReadOnly = true)]
    [Description("Reads an invoice from a ledger that is too slow.")]
    public static string Busy() => throw new TimeoutException("The ledger took too long.");

    // statuses nothing of the framework's described, as an endpoint's own middleware would send them
    [AgentTool("invoices_locked", ReadOnly = true)]
    [Description("Reads an invoice behind a gate the framework does not know about.")]
    public static string Locked() => throw new RpcException(new Status(StatusCode.PermissionDenied, "Locked by the ledger."));

    [AgentTool("invoices_gone", ReadOnly = true)]
    [Description("Reads an invoice that is gone, said only by status.")]
    public static string Gone() => throw new RpcException(new Status(StatusCode.NotFound, "Gone."));
}
