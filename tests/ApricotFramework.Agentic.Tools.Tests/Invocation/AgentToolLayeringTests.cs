using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Tests.Invocation;

/// <summary>Filters decide what exists for a caller; authorization decides what they may run.</summary>
public class AgentToolLayeringTests
{
    private static AgentToolInvoker Invoker(IAgentToolFilter filter, IAgentToolAuthorizationFilter authorization) =>
        new(new AgentToolRegistry([StaticAgentToolSource.For(new SingleProbe())]), [filter], [authorization]);

    [Fact]
    public async Task FilterRefuses_IsNotFound_AndAuthorizationIsNeverAsked()
    {
        // a tool out of scope does not exist for this caller, so there is nothing to authorize.
        // asking anyway would leak, through a 403, that it exists
        var authorization = new Authorization(allow: false);

        await Assert.ThrowsAsync<AgentToolFilteredException>(
            () => Invoker(new Filter(allow: false), authorization).InvokeCompleteAsync("probe_items_get", """{"id":1}""", Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal(0, authorization.Calls);
    }

    [Fact]
    public async Task FilterAllows_AuthorizationRefuses_IsAccessDenied_WithAuthorizationsReason()
    {
        var exception = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoker(new Filter(allow: true), new Authorization(allow: false)).InvokeCompleteAsync("probe_items_get", """{"id":1}""", Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Contains("not permitted", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BothRefuse_TheFilterWins()
    {
        var exception = await Assert.ThrowsAsync<AgentToolFilteredException>(
            () => Invoker(new Filter(allow: false), new Authorization(allow: false)).InvokeCompleteAsync("probe_items_get", """{"id":1}""", Probes.Context(), TestContext.Current.CancellationToken));

        Assert.Equal("out of scope", exception.Reason);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetAvailableToolsAsync_EitherLayerRefusing_LeavesTheToolOut(bool filter, bool authorization)
    {
        var tools = await Invoker(new Filter(filter), new Authorization(authorization)).GetAvailableToolsAsync(Probes.Context(), TestContext.Current.CancellationToken);

        Assert.Empty(tools);
    }

    [Fact]
    public async Task RegistrationOrder_DoesNotDecideTheOrderOfTheLayers()
    {
        // authorization registered first still runs second: the layering is the invoker's, not
        // the container's
        var services = new ServiceCollection();

        services.AddSingleton(new Authorization(allow: false));
        services.AddSingleton<IAgentToolAuthorizationFilter>(provider => provider.GetRequiredService<Authorization>());
        services.AddAgentToolsCore();
        services.AddAgentTool<SingleProbe>();
        services.AddSingleton<IAgentToolFilter>(new Filter(allow: false));

        var host = services.BuildServiceProvider();

        await Assert.ThrowsAsync<AgentToolFilteredException>(
            () => host.GetRequiredService<IAgentToolExecutor>().InvokeCompleteAsync("probe_items_get", """{"id":1}""", TestContext.Current.CancellationToken));

        Assert.Equal(0, host.GetRequiredService<Authorization>().Calls);
    }

    [Fact]
    public async Task ScopedFilters_AreBuiltOncePerCall()
    {
        // a filter reading a tenant off a scoped service needs the call's scope, not the root's
        var services = new ServiceCollection();

        services.AddSingleton<Built>();
        services.AddAgentToolsCore();
        services.AddAgentTool<SingleProbe>();
        services.AddAgentToolFilter<ScopedFilter>(ServiceLifetime.Scoped);
        services.AddAgentToolAuthorizationFilter<ScopedAuthorization>(ServiceLifetime.Scoped);

        var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var executor = host.GetRequiredService<IAgentToolExecutor>();

        await executor.InvokeCompleteAsync("probe_items_get", """{"id":1}""", TestContext.Current.CancellationToken);
        await executor.InvokeCompleteAsync("probe_items_get", """{"id":1}""", TestContext.Current.CancellationToken);

        var built = host.GetRequiredService<Built>();

        Assert.Equal(2, built.Filters);
        Assert.Equal(2, built.Authorizations);
    }

    /// <summary>A filter with a fixed answer.</summary>
    private sealed class Filter(bool allow) : IAgentToolFilter
    {
        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(allow ? AgentToolFilterDecision.Allow() : AgentToolFilterDecision.Deny("out of scope"));
    }

    /// <summary>An authorization filter with a fixed answer, which counts.</summary>
    private sealed class Authorization(bool allow) : IAgentToolAuthorizationFilter
    {
        /// <summary>Gets how many times it was consulted.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            this.Calls++;

            return ValueTask.FromResult(allow ? AgentToolAuthorizationDecision.Allow() : AgentToolAuthorizationDecision.Deny("not permitted"));
        }
    }

    /// <summary>Counts what the container built.</summary>
    private sealed class Built
    {
        /// <summary>Gets how many filters were built.</summary>
        public int Filters { get; set; }

        /// <summary>Gets how many authorization filters were built.</summary>
        public int Authorizations { get; set; }
    }

    /// <summary>A scoped filter that says when it is built.</summary>
    private sealed class ScopedFilter : IAgentToolFilter
    {
        public ScopedFilter(Built built) => built.Filters++;

        /// <inheritdoc />
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(AgentToolFilterDecision.Allow());
    }

    /// <summary>A scoped authorization filter that says when it is built.</summary>
    private sealed class ScopedAuthorization : IAgentToolAuthorizationFilter
    {
        public ScopedAuthorization(Built built) => built.Authorizations++;

        /// <inheritdoc />
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(AgentToolAuthorizationDecision.Allow());
    }
}
