using ApricotFramework.Agentic.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.ErrorDefinitions.Extensions;

/// <summary>
/// Registers the error definitions translator.
/// </summary>
public static class AgentToolErrorDefinitionsServiceCollectionExtensions
{
    /// <summary>
    /// Describes error definitions thrown by tools as agent tool failures.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <remarks>
    /// Translators are asked in registration order, so register a host's own more specific one first.
    /// </remarks>
    public static IServiceCollection AddAgentToolErrorDefinitions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAgentToolExceptionTranslator<ErrorDefinitionExceptionTranslator>(ServiceLifetime.Singleton);
    }
}
