using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// What the tickets service offers - under a name the billing service also uses, so federating
/// both needs a prefix.
/// </summary>
[AgentToolType]
public sealed class TicketTools
{
    /// <summary>Reads a ticket.</summary>
    /// <param name="id">The ticket.</param>
    /// <returns>Which ticket, from which service.</returns>
    [AgentTool("invoices_get", ReadOnly = true)]
    [Description("Reads the invoice a ticket is about.")]
    public static string Get(long id) => $"tickets:{id}";
}
