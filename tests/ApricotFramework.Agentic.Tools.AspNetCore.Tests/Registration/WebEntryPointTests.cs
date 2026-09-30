using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.AspNetCore.Invocation;
using ApricotFramework.Agentic.Tools.Invocation;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Registration;

/// <summary>
/// What a web host gets without asking, and what it can still say instead.
/// </summary>
public class WebEntryPointTests
{
    [Fact]
    public void Web_TakesTheCallerFromTheRequest()
    {
        // the reason this entry point exists: there is no host reachable over HTTP where
        // "nobody is calling" is the right answer, and forgetting to say so used to be silent
        var host = new ServiceCollection().AddAgentToolsWeb().Services.BuildServiceProvider();

        Assert.IsType<HttpAgentToolContextFactory>(host.GetRequiredService<IAgentToolContextFactory>());
    }

    [Fact]
    public void Core_TakesNobody()
    {
        var host = new ServiceCollection().AddAgentToolsCore().Services.BuildServiceProvider();

        Assert.IsType<DefaultAgentToolContextFactory>(host.GetRequiredService<IAgentToolContextFactory>());
    }

    [Fact]
    public void Web_ThenAnExplicitChoice_TakesTheExplicitOne()
    {
        // and because it is a chain, the other order cannot be written
        var host = new ServiceCollection()
            .AddAgentToolsWeb()
            .WithContext<DefaultAgentToolContextFactory>()
            .Services
            .BuildServiceProvider();

        Assert.IsType<DefaultAgentToolContextFactory>(host.GetRequiredService<IAgentToolContextFactory>());
    }

    [Fact]
    public void Web_DoesNotBringAuthorization()
    {
        // deliberate: it needs IAuthorizationService, and a host with open tools is real
        var host = new ServiceCollection().AddAgentToolsWeb().Services.BuildServiceProvider();

        Assert.Empty(host.GetServices<IAgentToolAuthorizationFilter>());
    }

    [Fact]
    public void WebWithAuthorization_BringsTheFilter()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();

        var host = services.AddAgentToolsWeb().WithAuthorization().Services.BuildServiceProvider();

        Assert.Single(host.GetServices<IAgentToolAuthorizationFilter>());
    }
}
