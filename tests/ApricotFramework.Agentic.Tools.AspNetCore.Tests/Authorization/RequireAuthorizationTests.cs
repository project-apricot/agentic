using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Options;
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

    private static AgentToolCreateOptions Options(string name) => new()
    {
        IsReadOnly = true,
        IsDestructive = false,
        Name = name
    };

    private static ServiceCollection Host()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorizationBuilder().AddPolicy("tools.use", policy => policy.RequireClaim("scope", "tools.use"));
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler("granted", "inherited"));
        services.AddAgentTools();
        services.AddAgentToolAuthorization();

        return services;
    }

    private static async Task Invoke(ServiceProvider host, string tool, ClaimsPrincipal? user)
    {
        await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            tool, """{"message":"hi"}""", new AgentToolContext { User = user },
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RequireAuthorization_NamedPolicy_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_policy")).RequireAuthorization("tools.use");

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_policy", Caller(new Claim("scope", "tools.use")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_policy", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_RequirementAttribute_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_attr")).RequireAuthorization(new RequireProbeAttribute("granted"));

        await Invoke(services.BuildServiceProvider(), "probe_echo_attr", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_RequirementDirectly_GatesAConstructedTool()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_req")).RequireAuthorization(new ProbeRequirement("granted"));

        await Invoke(services.BuildServiceProvider(), "probe_echo_req", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_RequirementNotGranted_Refuses()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_denied")).RequireAuthorization(new ProbeRequirement("not-granted"));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_echo_denied", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_WithoutArguments_RequiresAnAuthenticatedCaller()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_any")).RequireAuthorization();

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_any", Caller());

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_any", new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public async Task RequireAuthorization_TwoTermsOnOneTool_RequiresBoth()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_both"))
                .RequireAuthorization("tools.use")
                .RequireAuthorization(new ProbeRequirement("not-granted"));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_echo_both", Caller(new Claim("scope", "tools.use"))));
    }

    [Fact]
    public async Task RequireAuthorization_OnATypeThatAlreadyCarriesAnAttribute_AddsRatherThanReplaces()
    {
        var services = Host();

        // GatedProbe already requires "granted" and "inherited" by attribute
        services.AddAgentTool<GatedProbe>().RequireAuthorization(new ProbeRequirement("not-granted"));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(services.BuildServiceProvider(), "probe_items_gated", Caller()));
    }

    [Fact]
    public async Task RequireAuthorization_SatisfiesTheValidator_WhereNoAttributeWould()
    {
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_valid")).RequireAuthorization("tools.use");

        var tools = await services.BuildServiceProvider()
            .GetRequiredService<IAgentToolRegistry>()
            .GetToolsAsync(TestContext.Current.CancellationToken);

        Assert.Single(tools);
    }

    [Fact]
    public async Task ConstructedTool_WithNoAuthorizationAtAll_ComposesAndIsOpen()
    {
        // a constructed tool nobody gated is as open as a declared one nobody gated - the gate
        // comes from what the registration carries, not from how the tool was built
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_ungated"));

        var host = services.BuildServiceProvider();

        Assert.Single(await host.GetRequiredService<IAgentToolRegistry>().GetToolsAsync(TestContext.Current.CancellationToken));

        await Invoke(host, "probe_echo_ungated", Caller());
    }

    [Fact]
    public async Task RequireAuthorization_TwoToolsOfOneType_AreGatedSeparately()
    {
        // every tool adapted from a function shares one type, so keying on type alone would gate
        // them all together
        var services = Host();

        services.AddAgentTool(Echo(), Options("probe_echo_open")).RequireAuthorization(new ProbeRequirement("granted"));
        services.AddAgentTool(Echo(), Options("probe_echo_shut")).RequireAuthorization(new ProbeRequirement("not-granted"));

        var host = services.BuildServiceProvider();

        await Invoke(host, "probe_echo_open", Caller());

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(
            () => Invoke(host, "probe_echo_shut", Caller()));
    }
}
