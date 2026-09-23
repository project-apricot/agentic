using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Authorization;

/// <summary>Gating a tool that has no type to carry an attribute.</summary>
public class RequireAuthorizationTests
{
    private static ClaimsPrincipal Caller(params Claim[] claims) =>
        new(new ClaimsIdentity([new Claim("sub", "u1"), .. claims], "test", ClaimTypes.Name, ClaimTypes.Role));

    private static AIFunction Echo() => AIFunctionFactory.Create(
        ([Description("What to say back.")] string message) => message, "echo", "Says a message back.");

    private static AgentToolDeclaration Declaration(string name) => new()
    {
        Name = name,
        Description = "Says a message back.",
        IsReadOnly = true,
        IsDestructive = false
    };

    private static ServiceCollection Host()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorizationBuilder().AddPolicy("tools.use", policy => policy.RequireClaim("scope", "tools.use"));
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler("granted", "inherited"));
        services.AddAgentToolsCore().WithAuthorization();

        return services;
    }

    private static async Task Invoke(ServiceProvider host, string tool, ClaimsPrincipal? user)
    {
        await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            tool, """{"message":"hi"}""", Probes.Context(host, user),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RequireAuthorization_NamedPolicy_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_policy"), tool => tool.RequireAuthorization("tools.use"));

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_policy", Caller(new Claim("scope", "tools.use")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_policy", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_RequirementAttribute_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_attr"), tool => tool.RequireAuthorization(new RequireProbeAttribute("granted")));

        await Invoke(services.BuildServiceProvider(), "probe_echo_attr", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_RequirementDirectly_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_req"), tool => tool.RequireAuthorization(new ProbeRequirement("granted")));

        await Invoke(services.BuildServiceProvider(), "probe_echo_req", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_RequirementNotGranted_Refuses()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_denied"), tool => tool.RequireAuthorization(new ProbeRequirement("not-granted")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_echo_denied", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_WithoutArguments_RequiresAnAuthenticatedCaller()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_any"), tool => tool.RequireAuthorization());

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_any", Caller());

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_any", new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public async Task RequireAuthorization_TwoTermsOnOneTool_RequiresBoth()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_both"), tool => tool
            .RequireAuthorization("tools.use")
            .RequireAuthorization(new ProbeRequirement("not-granted")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_echo_both", Caller(new Claim("scope", "tools.use"))));
    }

    [Fact]
    public async Task RequireAuthorization_OnATypeThatAlreadyCarriesAnAttribute_AddsRatherThanReplaces()
    {
        var services = Host();

        // GatedProbe already requires "granted" and "inherited" by attribute
        services.AddAgentTool<GatedProbe>(tool => tool.RequireAuthorization(new ProbeRequirement("not-granted")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_items_gated", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_SatisfiesTheValidator_WhereNoAttributeWould()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_valid"), tool => tool.RequireAuthorization("tools.use"));

        var host = services.BuildServiceProvider();

        var tools = await host
            .GetRequiredService<IAgentToolRegistry>()
            .GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken);

        Assert.Single(tools);
    }

    [Fact]
    public async Task ConstructedTool_WithNoAuthorizationAtAll_ComposesAndIsOpen()
    {
        // a constructed tool nobody gated is as open as a declared one nobody gated - the gate
        // comes from what the registration carries, not from how the tool was built
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_ungated"));

        var host = services.BuildServiceProvider();

        Assert.Single(await host.GetRequiredService<IAgentToolRegistry>().GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));

        await Invoke(host, "probe_echo_ungated", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_TwoToolsOfOneType_AreGatedSeparately()
    {
        // every tool adapted from a function shares one type, so keying on type alone would gate
        // them all together
        var services = Host();

        services.AddAgentTool(Echo(), Declaration("probe_echo_open"), tool => tool.RequireAuthorization(new ProbeRequirement("granted")));
        services.AddAgentTool(Echo(), Declaration("probe_echo_shut"), tool => tool.RequireAuthorization(new ProbeRequirement("not-granted")));

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_open", Caller());

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_shut", Caller()));
    }
}
