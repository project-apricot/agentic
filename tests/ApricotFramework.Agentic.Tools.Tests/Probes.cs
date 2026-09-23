using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// What the tests share.
/// </summary>
public static class Probes
{
    /// <summary>
    /// A container with nothing in it.
    /// </summary>
    /// <remarks>
    /// A context now carries a scope rather than allowing none, because the executor always opens
    /// one. A test that does not care which scope still has to supply a provider, and this is it.
    /// </remarks>
    public static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    /// <summary>
    /// A caller nobody has anything to say about.
    /// </summary>
    /// <returns>The context.</returns>
    public static AgentToolContext Context() => new() { Services = Services };
}
