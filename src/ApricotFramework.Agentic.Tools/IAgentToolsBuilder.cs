using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Configures how this host composes and runs its tools.
/// </summary>
/// <remarks>
/// Holds only order-sensitive settings (context factory, gates, invocation decorators), which replace
/// or nest in the order written. Order-free registrations (tools, sources, validators, filters) are
/// <c>Add</c> methods on <see cref="IServiceCollection"/>.
/// </remarks>
public interface IAgentToolsBuilder
{
    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }
}
