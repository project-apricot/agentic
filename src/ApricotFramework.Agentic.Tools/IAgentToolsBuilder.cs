using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Configures how this host composes and runs its tools.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <c>AddAgentToolsCore</c> and by whatever entry point a host kind offers, and
/// carries the decisions where <strong>sequence is part of the meaning</strong>: who the caller
/// is, whether a gate applies, what wraps an invocation. Those either replace one another or
/// nest in the order written, so making them a chain is what stops the answer depending on which
/// file ran first.
/// </para>
/// <para>
/// Wiring that merely accumulates - a tool, a source, a validator, a filter - is not here. Those
/// are <c>Add</c> methods on <see cref="IServiceCollection"/>, because a host should be free to
/// put them in whatever file owns them and their order carries no meaning.
/// </para>
/// </remarks>
public interface IAgentToolsBuilder
{
    /// <summary>
    /// Gets the collection being configured.
    /// </summary>
    /// <remarks>
    /// The escape hatch, for a registration this builder has no method for.
    /// </remarks>
    IServiceCollection Services { get; }
}
