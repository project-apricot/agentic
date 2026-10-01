using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Adds metadata to a tool during registration.
/// </summary>
/// <remarks>
/// Only passed to <c>AddAgentTool</c> callbacks, never returned, so it cannot be mutated after
/// being read. For typed tools prefer attributes, which travel with the tool and are inherited.
/// </remarks>
public interface IAgentToolConventionBuilder
{
    /// <summary>
    /// Gets the service collection the tool is being registered into.
    /// </summary>
    /// <remarks>For conventions that need to register services of their own.</remarks>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the tool's metadata.
    /// </summary>
    /// <remarks>Becomes <see cref="AgentToolDescriptor.Metadata"/>.</remarks>
    IList<object> Metadata { get; }
}
