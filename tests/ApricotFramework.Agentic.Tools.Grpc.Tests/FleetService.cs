using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Grpc.Server.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// One service serving its tools over gRPC, in memory.
/// </summary>
public sealed class FleetService : IAsyncDisposable
{
    /// <summary>
    /// The running host.
    /// </summary>
    private readonly WebApplication app;

    /// <summary>
    /// Creates a new instance over a running host.
    /// </summary>
    /// <param name="app">The host.</param>
    /// <param name="gate">What it hides and refuses.</param>
    private FleetService(WebApplication app, Gate gate)
    {
        this.app = app;
        this.Gate = gate;
    }

    /// <summary>
    /// Gets what the service currently hides and refuses.
    /// </summary>
    public Gate Gate { get; }

    /// <summary>
    /// Gets the server, for routing a client to it.
    /// </summary>
    public TestServer Server => this.app.GetTestServer();

    /// <summary>
    /// Starts a service offering the tools of one type.
    /// </summary>
    /// <typeparam name="TTools">The tools.</typeparam>
    /// <returns>A task containing the running service.</returns>
    public static async Task<FleetService> StartAsync<TTools>() where TTools : class
    {
        var gate = new Gate();
        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(gate);
        builder.Services.AddAgentToolsCore();
        builder.Services.AddAgentToolType<TTools>();
        builder.Services.AddAgentToolFilter<Hiding>();
        builder.Services.AddAgentToolAuthorizationFilter<Refusing>();

        var app = builder.Build();

        app.MapAgentTools();

        await app.StartAsync().ConfigureAwait(false);

        return new FleetService(app, gate);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => this.app.DisposeAsync();

    /// <summary>Hides what the gate says to.</summary>
    /// <param name="gate">The gate.</param>
    private sealed class Hiding(Gate gate) : IAgentToolFilter
    {
        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(gate.Hides(tool.Name) ? AgentToolFilterDecision.Deny("hidden by the gate") : AgentToolFilterDecision.Allow());
    }

    /// <summary>Refuses what the gate says to.</summary>
    /// <param name="gate">The gate.</param>
    private sealed class Refusing(Gate gate) : IAgentToolAuthorizationFilter
    {
        /// <inheritdoc />
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(gate.Refuses(tool.Name) ? AgentToolAuthorizationDecision.Deny("Needs a supervisor.") : AgentToolAuthorizationDecision.Allow());
    }
}
