using ApricotFramework.Agentic.Tools.Invocation;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Tests.Registration;

/// <summary>
/// Which registrations care about order, and which deliberately do not.
/// </summary>
/// <remarks>
/// The split the API is built on: <c>Add</c> on the service collection is additive wiring a host
/// should be free to scatter across files, and <c>With</c> on the builder is a decision where
/// sequence is part of the meaning.
/// </remarks>
public class RegistrationOrderTests
{
    [Fact]
    public async Task AddingAToolBeforeTheCore_Works()
    {
        // the property that makes "put it in whatever file owns it" true. a tool registration
        // touches only the options and its own type, so it does not care whether the machinery
        // has been composed yet
        var services = new ServiceCollection();

        services.AddAgentToolType<MethodProbes>();
        services.AddScoped<ScopedDependency>();
        services.AddAgentToolsCore();

        Assert.NotEmpty(await Tools(services));
    }

    [Fact]
    public async Task AddingAToolAfterTheCore_Works()
    {
        var services = new ServiceCollection();

        services.AddAgentToolsCore();
        services.AddScoped<ScopedDependency>();
        services.AddAgentToolType<MethodProbes>();

        Assert.NotEmpty(await Tools(services));
    }

    [Fact]
    public void WithContext_LastInTheChainWins()
    {
        var host = new ServiceCollection()
            .AddAgentToolsCore()
            .WithContext<ProbeContextFactory>()
            .Services
            .BuildServiceProvider();

        Assert.IsType<ProbeContextFactory>(host.GetRequiredService<IAgentToolContextFactory>());
    }

    [Fact]
    public void WithoutAChoice_TheCoreAnswersNobody()
    {
        var host = new ServiceCollection().AddAgentToolsCore().Services.BuildServiceProvider();

        Assert.IsType<DefaultAgentToolContextFactory>(host.GetRequiredService<IAgentToolContextFactory>());
    }

    /// <summary>Composes whatever the collection holds.</summary>
    /// <param name="services">The collection.</param>
    /// <returns>A task containing the tools.</returns>
    private static async Task<IReadOnlyList<AgentToolDescriptor>> Tools(IServiceCollection services)
    {
        var host = services.BuildServiceProvider();

        return await host.GetRequiredService<IAgentToolRegistry>()
            .GetToolsAsync(new AgentToolContext { Services = host }, TestContext.Current.CancellationToken);
    }

    /// <summary>A context factory that is plainly not the default.</summary>
    private sealed class ProbeContextFactory : IAgentToolContextFactory
    {
        /// <inheritdoc />
        public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AgentToolContext { Services = scopedServices });
    }
}
