using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// What an entry point hands back.
/// </summary>
/// <param name="services">The collection being configured.</param>
internal sealed class AgentToolsBuilder(IServiceCollection services) : IAgentToolsBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; } = services;
}
