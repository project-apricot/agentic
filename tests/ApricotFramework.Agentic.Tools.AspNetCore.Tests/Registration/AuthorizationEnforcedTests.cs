using System.Security.Claims;
using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Registration;

/// <summary>There is no setting to turn authorization on, and a tripwire instead.</summary>
public class AuthorizationEnforcedTests
{
    private static ClaimsPrincipal Caller() => new(new ClaimsIdentity([new Claim("sub", "u1")], "test"));

    private static ServiceCollection Host()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler("granted", "inherited"));
        services.AddAgentToolsCore().WithAuthorization();

        return services;
    }

    [Fact]
    public async Task AddAgentTools_RegistersTheAuthorizationFilter_SoAGatedToolIsGated()
    {
        var services = Host();

        services.AddAgentTool<GatedProbe>();

        var host = services.BuildServiceProvider();

        // "granted" and "inherited" are held, so this passes
        await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            "probe_items_gated", null, Probes.Context(Probes.Empty, Caller()), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UngatedTool_IsOpenRatherThanRefused()
    {
        // a tool declaring nothing is allowed, exactly as a tool carrying no authorization
        // metadata is under the MCP SDK. the alternative offers no way to publish a public tool
        // beside gated ones, and surprises anyone who knows what no attribute means on a controller
        var services = Host();

        services.AddAgentTool<UngatedProbe>();

        var host = services.BuildServiceProvider();

        var result = await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            "probe_items_ungated", null, Probes.Context(Probes.Empty, Caller()), TestContext.Current.CancellationToken);

        Assert.Equal("\"ok\"", result);
    }

    [Fact]
    public async Task UngatedTool_IsOffered()
    {
        var services = Host();

        services.AddAgentTool<UngatedProbe>();

        var tools = await services.BuildServiceProvider().GetRequiredService<IAgentToolInvoker>()
            .GetAvailableToolsAsync(Probes.Context(Probes.Empty, Caller()), TestContext.Current.CancellationToken);

        Assert.Equal(["probe_items_ungated"], tools.Select(tool => tool.Name));
    }

    [Fact]
    public async Task UngatedTool_WithAFallbackPolicyConfigured_IsStillOpen()
    {
        // the fallback policy governs endpoints, and the transport endpoint carrying these calls
        // has already been through it. applying it again per tool would gate a tool on a policy
        // written for a route - so it is left alone, as the MCP SDK leaves it
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireClaim("never-held").Build());
        services.AddAgentToolsCore().WithAuthorization();
        services.AddAgentTool<UngatedProbe>();

        var host = services.BuildServiceProvider();

        var offered = await host.GetRequiredService<IAgentToolInvoker>().GetAvailableToolsAsync(
            Probes.Context(Probes.Empty, Caller()), TestContext.Current.CancellationToken);

        Assert.Equal(["probe_items_ungated"], offered.Select(tool => tool.Name));
    }

    [Fact]
    public async Task AnonymousTool_WaivesWhatItDeclares()
    {
        // [AllowAnonymous] beats an [Authorize] on the same tool, footgun and all. ASP.NET Core's
        // semantics, and the MCP SDK's
        var services = Host();

        services.AddAgentTool<AnonymousProbe>();

        var host = services.BuildServiceProvider();

        var offered = await host.GetRequiredService<IAgentToolInvoker>().GetAvailableToolsAsync(
            Probes.Context(Probes.Empty), TestContext.Current.CancellationToken);

        Assert.Equal(["probe_items_anonymous"], offered.Select(tool => tool.Name));

        var result = await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            "probe_items_anonymous", null, Probes.Context(Probes.Empty), TestContext.Current.CancellationToken);

        Assert.Equal("\"ok\"", result);
    }

    [Fact]
    public async Task AnonymousTool_WithoutAddAgentToolAuthorization_DoesNotTripTheWire()
    {
        // the tripwire is about a gate nothing enforces. a gate the host waived is not one
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddAgentToolsCore();
        services.AddAgentTool<AnonymousProbe>();

        var host = services.BuildServiceProvider();

        var registry = host.GetRequiredService<IAgentToolRegistry>();

        Assert.Single(await registry.GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GatedTool_WithoutAddAgentToolAuthorization_StopsTheHostRatherThanOpeningIt()
    {
        // the tripwire, and the realistic mistake: forgetting the call. a tool that declares
        // authorization while nothing enforces it is a configuration error, not a permission -
        // the same call MCP makes
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddAgentToolsCore();
        services.AddAgentTool<GatedProbe>();

        var host = services.BuildServiceProvider();

        var registry = host.GetRequiredService<IAgentToolRegistry>();

        var exception = await Assert.ThrowsAsync<AgentToolDeclarationException>(
            async () => await registry.GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));

        Assert.Contains("nothing in this host enforces it", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddAgentToolAuthorization", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GatedTool_WithoutAddAgentToolAuthorization_ButWithAMarker_IsLeftAlone()
    {
        // a host enforcing authorization its own way says so, and is not lectured about a job
        // somebody else is doing
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddAgentToolsCore();
        services.AddSingleton<AgentToolEnforcementMarker>();
        services.AddAgentTool<GatedProbe>();

        var host = services.BuildServiceProvider();

        var registry = host.GetRequiredService<IAgentToolRegistry>();

        Assert.Single(await registry.GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void WithoutAuthorizationConfigured_TheFilterIsNotBuiltAtAll()
    {
        // a host with tools and no policies never touches IAuthorizationService, so it gets an
        // answer rather than an unresolvable dependency
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAgentToolsCore();
        services.AddAgentTool<UngatedProbe>();

        Assert.Empty(services.BuildServiceProvider().GetServices<IAgentToolFilter>());
    }

    [Fact]
    public async Task UngatedTool_WithTheFilterRemoved_IsFineAndOpen()
    {
        // a host with genuinely open tools is a legitimate configuration; one that wrote
        // [Authorize] and did not wire it up is not
        var services = Host();

        services.AddAgentTool<UngatedProbe>();
        services.RemoveAll<IAgentToolFilter>();

        var host = services.BuildServiceProvider();

        Assert.Single(await host.GetRequiredService<IAgentToolRegistry>().GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));

        var result = await host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            "probe_items_ungated", null, Probes.Context(Probes.Empty), TestContext.Current.CancellationToken);

        Assert.Equal("\"ok\"", result);
    }

    [Fact]
    public async Task NoDeclarationChecksAreRegistered_SoAToolWithoutADescriptionIsAccepted()
    {
        // what a tool ought to carry beyond a name is the host's judgement
        var services = Host();

        services.AddAgentTool<TerseProbe>();

        var host = services.BuildServiceProvider();

        Assert.Single(await host.GetRequiredService<IAgentToolRegistry>()
            .GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken));
    }
}
