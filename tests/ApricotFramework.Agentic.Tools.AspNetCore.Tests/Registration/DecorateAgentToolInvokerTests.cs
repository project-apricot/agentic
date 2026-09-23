using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.AspNetCore.Tests;
using ApricotFramework.Agentic.Tools.Invocation;
using ApricotFramework.Agentic.Tools.Registry;
using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Registration;

/// <summary>Wrapping the registered invoker.</summary>
public class DecorateAgentToolInvokerTests
{
    private static ClaimsPrincipal Caller() =>
        new(new ClaimsIdentity([new Claim("sub", "u1")], "test"));

    private static IAgentToolsBuilder Host()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler>(new ProbeHandler("granted", "inherited"));
        services.AddAgentTool<GatedProbe>();

        return services.AddAgentToolsCore().WithAuthorization();
    }

    private static Task<string> Invoke(ServiceProvider host) =>
        host.GetRequiredService<IAgentToolInvoker>().InvokeCompleteAsync(
            "probe_items_gated", null, Probes.Context(Probes.Empty, Caller()),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Decorate_WrapsWhatWasRegistered()
    {
        var tools = Host();

        tools.Services.AddSingleton<Journal>();
        tools.DecorateInvoker<JournallingInvoker>();

        var host = tools.Services.BuildServiceProvider();

        await Invoke(host);

        Assert.Equal(["probe_items_gated"], host.GetRequiredService<Journal>().Entries);
    }

    [Fact]
    public async Task Decorate_TwiceOver_ReachesTheLastRegisteredFirst()
    {
        var tools = Host();

        tools.Services.AddSingleton<Journal>();
        tools.DecorateInvoker<InnerRecordingInvoker>();
        tools.DecorateInvoker<OuterRecordingInvoker>();

        var host = tools.Services.BuildServiceProvider();

        await Invoke(host);

        // the one registered last wraps the rest, so it sees the call first
        Assert.Equal(["outer", "inner"], host.GetRequiredService<Journal>().Entries);
    }

    [Fact]
    public async Task Decorate_StillResolvesTheRealInvokerUnderneath()
    {
        var tools = Host();

        tools.Services.AddSingleton<Journal>();
        tools.DecorateInvoker<JournallingInvoker>();

        Assert.Equal("\"ok\"", await Invoke(tools.Services.BuildServiceProvider()));
    }

    [Fact]
    public void Decorate_ResolvedAsASingleton_IsOneInstance()
    {
        var tools = Host();

        tools.Services.AddSingleton<Journal>();
        tools.DecorateInvoker<JournallingInvoker>();

        var host = tools.Services.BuildServiceProvider();

        Assert.Same(host.GetRequiredService<IAgentToolInvoker>(), host.GetRequiredService<IAgentToolInvoker>());
    }

    [Fact]
    public void Decorate_WrappingAnInstanceRegistration_Works()
    {
        // a descriptor can say what it provides three ways, and a wrapper has to cope with all
        var tools = Host();

        tools.Services.AddSingleton<Journal>();
        tools.Services.RemoveAll<IAgentToolInvoker>();
        tools.Services.AddSingleton<IAgentToolInvoker>(new AgentToolInvoker(
            new AgentToolRegistry([StaticAgentToolSource.For()]),
            null));

        tools.DecorateInvoker<JournallingInvoker>();

        Assert.IsType<JournallingInvoker>(tools.Services.BuildServiceProvider().GetRequiredService<IAgentToolInvoker>());
    }

    [Fact]
    public void Decorate_WithNothingRegistered_Throws()
    {
        var tools = new ServiceCollection().AddAgentToolsCore();

        tools.Services.RemoveAll<IAgentToolInvoker>();

        Assert.Throws<InvalidOperationException>(() => tools.DecorateInvoker<JournallingInvoker>());
    }

    /// <summary>Somewhere a wrapper can record what it saw.</summary>
    public sealed class Journal
    {
        /// <summary>Gets what was recorded.</summary>
        public List<string> Entries { get; } = [];
    }

    /// <summary>A wrapper that records each invocation, and takes a dependency from the container.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    /// <param name="journal">Where to record.</param>
    public sealed class JournallingInvoker(IAgentToolInvoker inner, Journal journal) : DelegatingAgentToolInvoker(inner)
    {
        /// <inheritdoc />
        public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            journal.Entries.Add(name);

            return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
        }
    }

    /// <summary>A wrapper recording that it was reached, so ordering is observable.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    /// <param name="journal">Where to record.</param>
    public sealed class InnerRecordingInvoker(IAgentToolInvoker inner, Journal journal) : DelegatingAgentToolInvoker(inner)
    {
        /// <inheritdoc />
        public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            journal.Entries.Add("inner");

            return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
        }
    }

    /// <summary>The other half of the pair above.</summary>
    /// <param name="inner">The invoker to pass calls to.</param>
    /// <param name="journal">Where to record.</param>
    public sealed class OuterRecordingInvoker(IAgentToolInvoker inner, Journal journal) : DelegatingAgentToolInvoker(inner)
    {
        /// <inheritdoc />
        public override Task<string> InvokeCompleteAsync(string name, string? argumentsJson, AgentToolContext context, CancellationToken cancellationToken = default)
        {
            journal.Entries.Add("outer");

            return base.InvokeCompleteAsync(name, argumentsJson, context, cancellationToken);
        }
    }
}
