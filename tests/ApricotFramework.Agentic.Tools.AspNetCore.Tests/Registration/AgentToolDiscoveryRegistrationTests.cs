using ApricotFramework.Agentic.Tools.AspNetCore.Authorization;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using ApricotFramework.Agentic.Tools.Discovery;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Registration;

/// <summary>Registering tools found by scanning.</summary>
public class AgentToolDiscoveryRegistrationTests
{
    private static readonly Assembly Here = typeof(GatedProbe).Assembly;

    private static ServiceCollection Host()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorizationBuilder().AddPolicy("tools.use", policy => policy.RequireClaim("scope", "tools.use"));
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler("granted", "inherited"));
        services.AddAgentToolsCore().WithAuthorization();

        return services;
    }

    private static async Task<IReadOnlyList<AgentToolDescriptor>> Compose(ServiceCollection services)
    {
        var host = services.BuildServiceProvider();

        return await host
            .GetRequiredService<IAgentToolRegistry>()
            .GetToolsAsync(Probes.Context(host), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddAgentToolsFromAssembly_RegistersWhatItFinds()
    {
        var services = Host();

        services.AddAgentToolsFromAssembly(Here, type => type == typeof(GatedProbe) || type == typeof(PolicyProbe));

        var tools = await Compose(services);

        Assert.Equal(
            ["probe_items_gated", "probe_items_policy"],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AddAgentToolsFromAssemblyContaining_LooksInThatTypesAssembly()
    {
        var services = Host();

        services.AddAgentToolsFromAssemblyContaining<GatedProbe>(type => type == typeof(RoleProbe));

        Assert.Equal("probe_items_role", Assert.Single(await Compose(services)).Name);
    }

    [Fact]
    public async Task AddAgentToolsFromAssemblies_RegistersEachToolOnce()
    {
        var services = Host();

        services.AddAgentToolTypes(AgentToolDiscovery.FromAssemblies([Here, Here], type => type == typeof(GatedProbe)));

        Assert.Single(await Compose(services));
    }

    [Fact]
    public async Task AddAgentTool_ByType_Registers()
    {
        var services = Host();

        services.AddAgentTool<GatedProbe>();

        Assert.Single(await Compose(services));
    }

    [Fact]
    public async Task AddAgentTool_ByType_StillTakesRequireAuthorization()
    {
        var services = Host();

        services.AddAgentTool<PolicyProbe>(tool => tool.RequireAuthorization(new ProbeRequirement("not-granted")));

        var host = services.BuildServiceProvider();

        await Assert.ThrowsAsync<Exceptions.AgentToolAccessDeniedException>(
            () => host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
                "probe_items_policy",
                null,
                Probes.Context(
                    host,
                    new System.Security.Claims.ClaimsPrincipal(
                        new System.Security.Claims.ClaimsIdentity(
                            [new System.Security.Claims.Claim("scope", "tools.use")], "test"))),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public void AddAgentTool_TypeThatIsNotATool_Throws()
    {
        var services = Host();

        // the point of the test is the wrong type, so the generic overload is not an option
#pragma warning disable CA2263
        Assert.Throws<ArgumentException>(() => services.AddAgentTool(typeof(string)));
#pragma warning restore CA2263
    }

    [Fact]
    public void AddAgentTool_AbstractTool_Throws()
    {
        var services = Host();

#pragma warning disable CA2263
        Assert.Throws<ArgumentException>(() => services.AddAgentTool(typeof(InheritingProbeBase)));
#pragma warning restore CA2263
    }

    [Fact]
    public async Task AddAgentToolsFromAssembly_SharedBaseClassAttribute_ReachesEveryToolFound()
    {
        // the answer to a group-wide requirement: an attribute on the base rather than a
        // registration per tool
        var services = Host();

        services.AddAgentToolsFromAssembly(Here, type => type == typeof(GatedProbe));

        var host = services.BuildServiceProvider();

        var tool = await host.GetRequiredService<IAgentToolRegistry>().RequireAsync("probe_items_gated", Probes.Context(host), TestContext.Current.CancellationToken);

        var names = AgentToolAuthorizationMetadata.For(tool).DeclaredRequirements.OfType<ProbeRequirement>().Select(requirement => requirement.Name);

        Assert.Contains("inherited", names);
    }

    [Fact]
    public async Task AddAgentToolSource_ByFactory_ComposesACuratedForeignSource()
    {
        // the shape a foreign source takes: wrapped, so one bad declaration costs it that tool
        var services = Host();

        // a foreign source attaches whatever should gate its tools, since nothing it fetches
        // carries an attribute
        services.AddAgentToolSource(_ => new CuratingAgentToolSource(
            StaticAgentToolSource.For(new GatedProbe(), new TerseProbe()),
            AgentToolCuration.DropRejected(
                [new ApricotFramework.Agentic.Tools.Validators.DescriptionDeclaredValidator()])));

        var tools = await Compose(services);

        // TerseProbe describes itself with whitespace, so the check leaves it out - and only it
        Assert.Equal("probe_items_gated", Assert.Single(tools).Name);
    }
}
