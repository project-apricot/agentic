using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Mcp.Server.Tests;

/// <summary>Tools to call over MCP: one that works, one whose record is missing, one that is gated.</summary>
[AgentToolType]
public sealed class InvoiceTools
{
    [AgentTool("invoices_get", Title = "Get an invoice", ReadOnly = true)]
    [Description("Reads an invoice.")]
    public static string Get([Description("The invoice.")] long id) => $"invoice:{id}";

    [AgentTool("invoices_missing", Title = "Get a missing invoice", ReadOnly = true)]
    [Description("Reads an invoice that is not there.")]
    public static string Missing([Description("The invoice.")] long id) => throw new KeyNotFoundException($"No invoice {id}.");

    [AgentTool("invoices_broken", Title = "Read from a broken ledger", ReadOnly = true)]
    [Description("Fails with something nobody described.")]
    public static string Broken() => throw new InvalidOperationException("connection string Server=ledger;Password=hunter2 rejected");

    [AgentTool("invoices_void", Title = "Void an invoice", Destructive = true)]
    [Description("Voids an invoice.")]
    public static string Void([Description("The invoice.")] long id) => $"voided:{id}";
}
