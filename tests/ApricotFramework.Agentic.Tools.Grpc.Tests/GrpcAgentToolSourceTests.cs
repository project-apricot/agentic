using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.Grpc.Client.Extensions;
using ApricotFramework.Agentic.Tools.Grpc.Client;
using ApricotFramework.Agentic.Tools.Grpc.Client.Extensions;
using ApricotFramework.Agentic.Tools.Grpc.Contract;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// Federating several services over one contract, and holding no client while doing it.
/// </summary>
/// <param name="fleet">The services.</param>
public class GrpcAgentToolSourceTests(Fleet fleet) : IClassFixture<Fleet>
{
    private readonly ClientLog clients = new();

    private ServiceProvider Host(ClientRegistration how, Action<GrpcAgentToolSourceOptions>? billing = null, ErrorMapping mapping = ErrorMapping.None)
    {
        var services = new ServiceCollection();

        if (mapping == ErrorMapping.ContainerWide)
        {
            services.AddGrpcErrorMapping();
        }

        services.AddSingleton(this.clients);
        services.AddScoped<CallScope>();
        services.AddAgentToolsCore();

        if (how == ClientRegistration.Typed)
        {
            Route(services.AddGrpcClient<BillingAgentToolsClient>(options => options.Address = fleet.Billing.Server.BaseAddress), "billing", fleet.Billing, mapping);
            Route(services.AddGrpcClient<TicketsAgentToolsClient>(options => options.Address = fleet.Tickets.Server.BaseAddress), "tickets", fleet.Tickets, mapping);

            services.AddGrpcAgentTools<BillingAgentToolsClient>(options => Billing(options, billing));
            services.AddGrpcAgentTools<TicketsAgentToolsClient>(options => options.Prefix = "tickets_");
        }
        else
        {
            Route(services.AddGrpcClient<AgentTools.AgentToolsClient>("billing", options => options.Address = fleet.Billing.Server.BaseAddress), "billing", fleet.Billing, mapping);
            Route(services.AddGrpcClient<AgentTools.AgentToolsClient>("tickets", options => options.Address = fleet.Tickets.Server.BaseAddress), "tickets", fleet.Tickets, mapping);

            services.AddGrpcAgentTools("billing", options => Billing(options, billing));
            services.AddGrpcAgentTools("tickets", options => options.Prefix = "tickets_");
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void Billing(GrpcAgentToolSourceOptions options, Action<GrpcAgentToolSourceOptions>? more)
    {
        options.Prefix = "billing_";
        more?.Invoke(options);
    }

    // what the host does on its own client registration, which is the point: none of it is ours
    private static void Route(IHttpClientBuilder client, string service, FleetService to, ErrorMapping mapping)
    {
        client.ConfigurePrimaryHttpMessageHandler(() => to.Server.CreateHandler())
            .AddInterceptor(InterceptorScope.Client, provider => new Recording(service, provider));

        if (mapping == ErrorMapping.OnTheClient)
        {
            client.AddGrpcErrorMapping();
        }
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task TwoServices_OneContract_AreListedAndReachedSeparately(ClientRegistration how)
    {
        // the same contract, the same remote tool name, two channels
        var executor = Host(how).GetRequiredService<IAgentToolExecutor>();

        var tools = await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["billing_invoices_fail", "billing_invoices_get", "tickets_invoices_get"], tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));

        Assert.Equal("\"billing:7\"", await executor.InvokeCompleteAsync("billing_invoices_get", """{"id":7}""", TestContext.Current.CancellationToken));
        Assert.Equal("\"tickets:7\"", await executor.InvokeCompleteAsync("tickets_invoices_get", """{"id":7}""", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task EveryListing_GetsItsOwnClient(ClientRegistration how)
    {
        // the lifetime claim. a source that held its client would show one per service here, for
        // good, and pin the handler the factory means to rotate
        var executor = Host(how).GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);
        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, this.clients.For("billing"));
        Assert.Equal(2, this.clients.For("tickets"));
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task ACachedListing_HoldsNoClient(ClientRegistration how)
    {
        // the listing is remembered; the client it was read with is not. a cached tool invoked
        // later still gets a client of its own
        var executor = Host(how, billing => billing.Lifetime = TimeSpan.FromHours(1)).GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);
        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, this.clients.For("billing"));

        await executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken);

        Assert.Equal(2, this.clients.For("billing"));
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task Clients_AreMadeInTheCallsScope(ClientRegistration how)
    {
        // two calls, two scopes - and within one call the listing and the invocation share it
        var executor = Host(how).GetRequiredService<IAgentToolExecutor>();

        await executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken);
        await executor.InvokeCompleteAsync("billing_invoices_get", """{"id":2}""", TestContext.Current.CancellationToken);

        Assert.Equal(2, this.clients.Made.Select(entry => entry.Scope).Distinct().Count());
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task RefusedByTheServicesAuthorization_ArrivesAsAccessDenied(ClientRegistration how)
    {
        // listed while it was allowed, refused by the time it is called. the refusal stays a
        // refusal across the hop
        var executor = Host(how, billing => billing.Lifetime = TimeSpan.FromHours(1)).GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        fleet.Billing.Gate.Refuse("invoices_get");

        try
        {
            var exception = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
                () => executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken));

            Assert.Contains("supervisor", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            fleet.Billing.Gate.Reset();
        }
    }

    [Theory]
    [InlineData(ClientRegistration.Typed)]
    [InlineData(ClientRegistration.Named)]
    public async Task HiddenByTheServicesFilter_ArrivesAsNotFound_WithoutItsReason(ClientRegistration how)
    {
        var executor = Host(how, billing => billing.Lifetime = TimeSpan.FromHours(1)).GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        fleet.Billing.Gate.Hide("invoices_get");

        try
        {
            var exception = await Assert.ThrowsAsync<AgentToolNotFoundException>(
                () => executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken));

            Assert.DoesNotContain("gate", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            fleet.Billing.Gate.Reset();
        }
    }

    [Theory]
    [InlineData(ErrorMapping.OnTheClient)]
    [InlineData(ErrorMapping.ContainerWide)]
    public async Task WithTheHostsErrorMapping_ARefusalIsStillARefusal(ErrorMapping mapping)
    {
        // the host translates transport failures into its own error type, possibly for every
        // client at once. the refusal is read from the chain, and the host's error rides along
        var executor = Host(ClientRegistration.Typed, billing => billing.Lifetime = TimeSpan.FromHours(1), mapping).GetRequiredService<IAgentToolExecutor>();

        await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        try
        {
            fleet.Billing.Gate.Refuse("invoices_get");

            var denied = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
                () => executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken));

            Assert.Contains("supervisor", denied.Message, StringComparison.Ordinal);
            Assert.IsType<ErrorDefinitionException>(denied.InnerException);

            fleet.Billing.Gate.Reset();
            fleet.Billing.Gate.Hide("invoices_get");

            await Assert.ThrowsAsync<AgentToolNotFoundException>(
                () => executor.InvokeCompleteAsync("billing_invoices_get", """{"id":1}""", TestContext.Current.CancellationToken));
        }
        finally
        {
            fleet.Billing.Gate.Reset();
        }
    }

    [Theory]
    [InlineData(ErrorMapping.None)]
    [InlineData(ErrorMapping.OnTheClient)]
    [InlineData(ErrorMapping.ContainerWide)]
    public async Task AFault_IsLeftAsTheHostWouldHaveIt(ErrorMapping mapping)
    {
        // nothing of this library's describes a fault, so the call fails with whatever the host's
        // client failed with - its own error type where it translates, the transport's where not
        var executor = Host(ClientRegistration.Typed, mapping: mapping).GetRequiredService<IAgentToolExecutor>();

        var thrown = await Record.ExceptionAsync(
            () => executor.InvokeCompleteAsync("billing_invoices_fail", null, TestContext.Current.CancellationToken));

        Assert.IsType(mapping == ErrorMapping.None ? typeof(RpcException) : typeof(ErrorDefinitionException), thrown);
    }

    [Fact]
    public async Task ATypeNeverRegisteredAsAClient_SaysHowToRegisterIt()
    {
        var services = new ServiceCollection();

        services.AddAgentToolsCore();
        services.AddGrpcAgentTools<BillingAgentToolsClient>();

        var executor = services.BuildServiceProvider().GetRequiredService<IAgentToolExecutor>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken));

        Assert.Contains("AddGrpcClient<BillingAgentToolsClient>()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ANameNeverRegistered_IsNamedByTheFactory()
    {
        var services = new ServiceCollection();

        services.AddGrpcClient<AgentTools.AgentToolsClient>("billing", options => options.Address = new Uri("https://billing"));
        services.AddAgentToolsCore();
        services.AddGrpcAgentTools("biling");

        var executor = services.BuildServiceProvider().GetRequiredService<IAgentToolExecutor>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.GetAvailableToolsAsync(TestContext.Current.CancellationToken));

        Assert.Contains("'biling'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokedByHandWithoutServices_SaysWhatIsMissing()
    {
        // the client lives in the container, and there is nowhere else to get one
        using var host = Host(ClientRegistration.Typed);
        using var scope = host.CreateScope();

        var tools = await host.GetRequiredService<IAgentToolRegistry>().GetToolsAsync(new AgentToolContext { Services = scope.ServiceProvider }, TestContext.Current.CancellationToken);
        var function = Assert.IsType<RemoteAgentTool>(tools.Single(tool => tool.Name == "billing_invoices_get").Tool);

        await Assert.ThrowsAsync<AgentToolNotInvocableException>(
            async () => await function.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?> { ["id"] = 1 }), TestContext.Current.CancellationToken));
    }

    /// <summary>Records each client as the factory makes it.</summary>
    private sealed class Recording : Interceptor
    {
        public Recording(string service, IServiceProvider provider) =>
            provider.GetRequiredService<ClientLog>().Record(service, provider.GetRequiredService<CallScope>().Id);
    }
}
