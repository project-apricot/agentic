using ApricotFramework.Agentic.Tools.Adapters;
using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using ApricotFramework.Agentic.Tools.Validators;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Authorization;

/// <summary>Deciding a tool from what it declares - a requirement attribute, a named policy, or a role.</summary>
public class AgentToolAuthorizationTests
{
    private static AgentToolContext Asking(ClaimsPrincipal? user) => Probes.Context(Probes.Empty, user);

    private static ClaimsPrincipal Caller(params Claim[] claims) =>
        new(new ClaimsIdentity([new Claim("sub", "u1"), .. claims], "test", ClaimTypes.Name, ClaimTypes.Role));

    private static ServiceProvider Host(params string[] grantedProbes)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorizationBuilder().AddPolicy("tools.use", policy => policy.RequireClaim("scope", "tools.use"));
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler(grantedProbes));
        services.AddAgentToolsCore().WithAuthorization();
        services.AddAgentTool<GatedProbe>();
        services.AddAgentTool<PolicyProbe>();
        services.AddAgentTool<RoleProbe>();
        services.AddAgentTool<MixedProbe>();

        return services.BuildServiceProvider();
    }

    private static async Task Authorize(ServiceProvider host, string tool, ClaimsPrincipal? user)
    {
        var declared = await host.GetRequiredService<IAgentToolRegistry>().RequireAsync(tool, Probes.Context(host), TestContext.Current.CancellationToken);

        var decision = await host.GetServices<IAgentToolFilter>().OfType<AuthorizationAgentToolFilter>().Single()
            .EvaluateAsync(declared, Asking(user), TestContext.Current.CancellationToken);

        if (!decision.IsAllowed)
        {
            throw new Tools.Exceptions.AgentToolAccessDeniedException(decision.Reason!);
        }
    }

    [Fact]
    public async Task Authorize_RequirementAttributeSatisfied_Permits()
    {
        await Authorize(Host("granted", "inherited"), "probe_items_gated", Caller());
    }

    [Fact]
    public async Task Authorize_NamedPolicySatisfied_Permits()
    {
        await Authorize(Host(), "probe_items_policy", Caller(new Claim("scope", "tools.use")));
    }

    [Fact]
    public async Task Authorize_NamedPolicyNotSatisfied_Refuses()
    {
        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Authorize(Host(), "probe_items_policy", Caller()));
    }

    [Fact]
    public async Task Authorize_RoleSatisfied_Permits()
    {
        await Authorize(Host(), "probe_items_role", Caller(new Claim(ClaimTypes.Role, "curator")));
    }

    [Fact]
    public async Task Authorize_RoleNotSatisfied_Refuses()
    {
        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Authorize(Host(), "probe_items_role", Caller(new Claim(ClaimTypes.Role, "reader"))));
    }

    [Fact]
    public async Task Authorize_BothMechanismsDeclared_RequiresBoth()
    {
        var withPolicyOnly = Caller(new Claim("scope", "tools.use"));

        // the named policy is satisfied, the requirement attribute is not
        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Authorize(Host(), "probe_items_mixed", withPolicyOnly));

        // and with both, it passes
        await Authorize(Host("granted"), "probe_items_mixed", withPolicyOnly);
    }

    [Fact]
    public async Task Authorize_BothMechanismsDeclaredAndOnlyTheAttributeSatisfied_Refuses()
    {
        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Authorize(Host("granted"), "probe_items_mixed", Caller()));
    }

    [Fact]
    public async Task Authorize_NoCaller_Refuses()
    {
        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Authorize(Host("granted", "inherited"), "probe_items_gated", null));
    }

    [Fact]
    public async Task Authorize_DescriptorIsPassedAsTheResource_SoAHandlerCanNarrowOnIt()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();

        var handler = new ResourceCapturingHandler();

        services.AddSingleton<IAuthorizationHandler>(handler);
        services.AddAgentToolsCore().WithAuthorization();
        services.AddAgentTool<GatedProbe>();

        var host = services.BuildServiceProvider();

        await Authorize(host, "probe_items_gated", Caller());

        // the descriptor, not the tool: a handler that wants to narrow on "destructive" or on a
        // label reads the declaration, and one that wants what the host said reads the metadata.
        // handing it only the tool would have closed off the second
        var resource = Assert.IsType<AgentToolDescriptor>(handler.Resource);

        Assert.Equal("probe_items_gated", resource.Name);

        // and a class-per-tool is no longer reachable by its own type from a listing, because no
        // instance of it exists until somebody calls it. what it declares is
        Assert.False(resource.Declaration.IsDestructive);
        Assert.Equal(typeof(GatedProbe), Assert.IsType<ScopedAgentTool>(resource.Tool).ToolType);
    }



    [Fact]
    public async Task GetAvailableToolsAsync_OffersOnlyWhatTheCallerSatisfies()
    {
        // GatedProbe needs "granted" and "inherited"; PolicyProbe needs the tools.use policy;
        // RoleProbe needs the curator role; MixedProbe needs both of the first two
        var host = Host("granted", "inherited");

        var tools = await host.GetRequiredService<IAgentToolInvoker>().GetAvailableToolsAsync(
            Asking(Caller(new Claim("scope", "tools.use"))), TestContext.Current.CancellationToken);

        Assert.Equal(
            ["probe_items_gated", "probe_items_mixed", "probe_items_policy"],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAvailableToolsAsync_CallerSatisfyingNothing_IsOfferedNothing()
    {
        var host = Host();

        var tools = await host.GetRequiredService<IAgentToolInvoker>().GetAvailableToolsAsync(
            Asking(Caller()), TestContext.Current.CancellationToken);

        Assert.Empty(tools);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_NoCaller_IsOfferedNothing()
    {
        var host = Host("granted", "inherited");

        var tools = await host.GetRequiredService<IAgentToolInvoker>().GetAvailableToolsAsync(
            Asking(null), TestContext.Current.CancellationToken);

        Assert.Empty(tools);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_EveryToolOffered_IsOneTheGateAccepts()
    {
        var host = Host("granted", "inherited");
        var invoker = host.GetRequiredService<IAgentToolInvoker>();
        var user = Caller(new Claim("scope", "tools.use"));

        var offered = await invoker.GetAvailableToolsAsync(
            Asking(user), TestContext.Current.CancellationToken);

        Assert.NotEmpty(offered);

        foreach (var tool in offered)
        {
            await invoker.InvokeCompleteAsync(tool.Name, null, Asking(user), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AddAgentTools_EndToEnd_GatesTheCallOnWhatTheToolDeclares()
    {
        var host = Host();
        var invoker = host.GetRequiredService<IAgentToolInvoker>();

        var result = await invoker.InvokeCompleteAsync(
            "probe_items_policy", null, Asking(Caller(new Claim("scope", "tools.use"))), TestContext.Current.CancellationToken);

        Assert.Equal("\"ok\"", result);

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => invoker.InvokeCompleteAsync("probe_items_policy", null, Asking(Caller()), TestContext.Current.CancellationToken));
    }

    /// <summary>Records the resource it was handed.</summary>
    private sealed class ResourceCapturingHandler : AuthorizationHandler<ProbeRequirement>
    {
        /// <summary>Gets the resource it last saw.</summary>
        public object? Resource { get; private set; }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProbeRequirement requirement)
        {
            this.Resource = context.Resource;

            context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
