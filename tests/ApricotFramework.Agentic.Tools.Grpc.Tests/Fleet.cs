namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// Two services implementing the same contract, started once for the tests that federate them.
/// </summary>
public sealed class Fleet : IAsyncLifetime
{
    /// <summary>
    /// Gets the billing service.
    /// </summary>
    public FleetService Billing { get; private set; } = null!;

    /// <summary>
    /// Gets the tickets service.
    /// </summary>
    public FleetService Tickets { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        this.Billing = await FleetService.StartAsync<BillingTools>();
        this.Tickets = await FleetService.StartAsync<TicketTools>();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await this.Billing.DisposeAsync();
        await this.Tickets.DisposeAsync();
    }
}
