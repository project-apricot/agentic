using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using ApricotFramework.Agentic.Tools.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApricotFramework.Agentic.Tools.Tests.Registration;

/// <summary>
/// Whatever runs per call can depend on what is scoped, under the validation a development host
/// turns on - which is where a captured scoped service is caught rather than shipped.
/// </summary>
public class LifetimeTests
{
    private static readonly ServiceProviderOptions Strict = new() { ValidateScopes = true, ValidateOnBuild = true };

    [Fact]
    public async Task AContextFactoryDependingOnSomethingScoped_BuildsAndRunsPerCall()
    {
        var services = new ServiceCollection();

        services.AddScoped<PerCall>();
        services.AddAgentToolsCore().WithContext<ScopedContextFactory>();
        services.AddAgentToolType<Tools>();

        await using var host = services.BuildServiceProvider(Strict);

        var executor = host.GetRequiredService<IAgentToolExecutor>();

        var first = await executor.InvokeCompleteAsync("lifetime_caller", """{"note":"a"}""", TestContext.Current.CancellationToken);
        var second = await executor.InvokeCompleteAsync("lifetime_caller", """{"note":"b"}""", TestContext.Current.CancellationToken);

        Assert.NotEqual(first, second);
        Assert.StartsWith("\"scope-", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupValidation_ResolvesAScopedContextFactory()
    {
        var services = new ServiceCollection();

        services.AddScoped<PerCall>();
        services.AddAgentToolsCore().WithContext<ScopedContextFactory>();
        services.AddAgentToolType<Tools>();

        await using var host = services.BuildServiceProvider(Strict);

        foreach (var hosted in host.GetServices<IHostedService>().OfType<AgentToolStartupValidation>())
        {
            await hosted.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task FiltersDependingOnSomethingScoped_AreScopedByDefault_AndBuild()
    {
        var services = new ServiceCollection();

        services.AddScoped<PerCall>();
        services.AddAgentToolsCore();
        services.AddAgentToolType<Tools>();
        services.AddAgentToolFilter<ScopedFilter>();
        services.AddAgentToolAuthorizationFilter<ScopedAuthorization>();

        Assert.All(
            services.Where(descriptor => descriptor.ServiceType == typeof(IAgentToolFilter) || descriptor.ServiceType == typeof(IAgentToolAuthorizationFilter)),
            descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));

        await using var host = services.BuildServiceProvider(Strict);

        Assert.Equal("3", await host.GetRequiredService<IAgentToolExecutor>().InvokeCompleteAsync("lifetime_add", """{"left":1,"right":2}""", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ATranslatorIsScopedByDefault()
    {
        var services = new ServiceCollection();

        services.AddAgentToolExceptionTranslator<NoTranslation>();

        Assert.Equal(ServiceLifetime.Scoped, services.Single(descriptor => descriptor.ServiceType == typeof(IAgentToolExceptionTranslator)).Lifetime);
    }

    private sealed class ScopedContextFactory(PerCall dependency) : IAgentToolContextFactory
    {
        public ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AgentToolContext
            {
                Services = scopedServices,
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"scope-{dependency.Ordinal}")], "test"))
            });
    }

    private sealed class ScopedFilter(PerCall dependency) : IAgentToolFilter
    {
        public ValueTask<AgentToolFilterDecision> EvaluateAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(dependency.Ordinal > 0 ? AgentToolFilterDecision.Allow() : AgentToolFilterDecision.Deny("never"));
    }

    private sealed class ScopedAuthorization(PerCall dependency) : IAgentToolAuthorizationFilter
    {
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(dependency.Ordinal > 0 ? AgentToolAuthorizationDecision.Allow() : AgentToolAuthorizationDecision.Deny("never"));
    }

    // its own, rather than the shared probe dependency, whose counters other classes assert on
    private sealed class PerCall
    {
        private static int built;

        public int Ordinal { get; } = Interlocked.Increment(ref built);
    }

    [AgentToolType]
    private sealed class Tools
    {
        [AgentTool("lifetime_caller", ReadOnly = true)]
        [System.ComponentModel.Description("Reports who is asking.")]
        public static string Caller(AgentToolContext context, [System.ComponentModel.Description("A note.")] string note) =>
            $"{context.User?.Identity?.Name ?? "nobody"}:{note}";

        [AgentTool("lifetime_add", ReadOnly = true)]
        [System.ComponentModel.Description("Adds two numbers together.")]
        public static int Add([System.ComponentModel.Description("The first number.")] int left, [System.ComponentModel.Description("The second number.")] int right) => left + right;
    }

    private sealed class NoTranslation : IAgentToolExceptionTranslator
    {
        public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool) => null;
    }
}
