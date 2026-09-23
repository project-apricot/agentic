using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests;

/// <summary>
/// What the tests share.
/// </summary>
public static class Probes
{
    /// <summary>
    /// A caller, and the scope its call runs in.
    /// </summary>
    /// <param name="services">The scope, which in a host under test is the host itself.</param>
    /// <param name="user">Who is asking, or null for nobody.</param>
    /// <returns>The context.</returns>
    public static AgentToolContext Context(IServiceProvider services, ClaimsPrincipal? user = null) =>
        new() { Services = services, User = user };

    /// <summary>
    /// A container with nothing in it.
    /// </summary>
    public static IServiceProvider Empty { get; } = new ServiceCollection().BuildServiceProvider();
}
