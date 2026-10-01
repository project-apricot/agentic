using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// The <see cref="IAgentToolsBuilder"/> returned by entry points.
/// </summary>
/// <param name="services">The service collection.</param>
internal sealed class AgentToolsBuilder(IServiceCollection services) : IAgentToolsBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; } = services;
}
