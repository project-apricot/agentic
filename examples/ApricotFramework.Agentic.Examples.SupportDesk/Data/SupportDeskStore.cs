using ApricotFramework.Agentic.Examples.SupportDesk.Model;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Data;

/// <summary>
/// The support desk's data, in memory.
/// </summary>
/// <remarks>
/// Stands in for whatever a real application reads through. The tools call this the way they would
/// call an application service - the point of the example is the declaration and the gate, not the
/// storage.
/// </remarks>
public sealed class SupportDeskStore
{
    private readonly List<Ticket> tickets;

    private readonly List<Customer> customers;

    private long nextTicketId = 4;

    /// <summary>Creates a store with something in it.</summary>
    public SupportDeskStore()
    {
        this.customers =
        [
            new Customer(1, "Ada Okonkwo", "ada@example.org", "Northwind"),
            new Customer(2, "Bruno Salas", "bruno@example.org", "Contoso"),
            new Customer(3, "Chen Wei", "chen@example.org", null)
        ];

        this.tickets =
        [
            new Ticket(1, "Cannot sign in", "The password reset email never arrives.", TicketStatus.Open, "ada.support", 1, null, ["Asked for the address they used."], DateTime.UtcNow.AddDays(-3)),
            new Ticket(2, "Invoice is wrong", "We were billed twice in March.", TicketStatus.Pending, "bruno.support", 2, null, [], DateTime.UtcNow.AddDays(-12)),
            new Ticket(3, "Export times out", "The CSV export hangs at 80%.", TicketStatus.Closed, "ada.support", 3, "Raised the timeout and reran it.", ["Reproduced on staging."], DateTime.UtcNow.AddDays(-90))
        ];
    }

    /// <summary>Every ticket, newest first.</summary>
    /// <returns>The tickets.</returns>
    public IReadOnlyList<Ticket> Tickets() => [.. this.tickets.OrderByDescending(ticket => ticket.Raised)];

    /// <summary>Every customer.</summary>
    /// <returns>The customers.</returns>
    public IReadOnlyList<Customer> Customers() => this.customers;

    /// <summary>Finds a ticket.</summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The ticket, or null.</returns>
    public Ticket? Ticket(long id) => this.tickets.FirstOrDefault(ticket => ticket.Id == id);

    /// <summary>Finds a customer.</summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The customer, or null.</returns>
    public Customer? Customer(long id) => this.customers.FirstOrDefault(customer => customer.Id == id);

    /// <summary>How many tickets a customer has raised.</summary>
    /// <param name="customerId">The customer.</param>
    /// <returns>The count.</returns>
    public int TicketCount(long customerId) => this.tickets.Count(ticket => ticket.CustomerId == customerId);

    /// <summary>Raises a ticket.</summary>
    /// <param name="subject">The summary.</param>
    /// <param name="body">The detail.</param>
    /// <param name="customerId">Who raised it.</param>
    /// <returns>The new ticket.</returns>
    public Ticket Create(string subject, string body, long customerId)
    {
        var ticket = new Ticket(this.nextTicketId++, subject, body, TicketStatus.Open, null, customerId, null, [], DateTime.UtcNow);

        this.tickets.Add(ticket);

        return ticket;
    }

    /// <summary>Replaces a ticket.</summary>
    /// <param name="ticket">The ticket to store.</param>
    public void Replace(Ticket ticket)
    {
        var index = this.tickets.FindIndex(entry => entry.Id == ticket.Id);

        this.tickets[index] = ticket;
    }

    /// <summary>Removes a ticket.</summary>
    /// <param name="id">The identifier.</param>
    /// <returns>True where one went.</returns>
    public bool Delete(long id) => this.tickets.RemoveAll(ticket => ticket.Id == id) > 0;

    /// <summary>Removes closed tickets older than a cutoff.</summary>
    /// <param name="cutoff">The cutoff.</param>
    /// <returns>How many went.</returns>
    public int PurgeClosedBefore(DateTime cutoff) =>
        this.tickets.RemoveAll(ticket => ticket.Status == TicketStatus.Closed && ticket.Raised < cutoff);
}
