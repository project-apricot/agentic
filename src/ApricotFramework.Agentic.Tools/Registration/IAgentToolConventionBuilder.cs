using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Saying something about a tool, while it is being registered.
/// </summary>
/// <remarks>
/// <para>
/// Handed to the callback an <c>AddAgentTool</c> overload takes, and not returned from it. A
/// builder that outlives its call is one that can be mutated after the thing it configures has
/// been read, and the reading happens in a different phase from the writing - which is a shape
/// worth not having.
/// </para>
/// <para>
/// For a typed tool, attributes on the class are usually the better place: they travel with the
/// tool rather than with one composition root, and they reach every tool deriving from a shared
/// base. This is for what has no class to carry one.
/// </para>
/// </remarks>
public interface IAgentToolConventionBuilder
{
    /// <summary>
    /// Gets the collection the tool is being registered into.
    /// </summary>
    /// <remarks>
    /// For a convention that has to register something of its own alongside - a handler, a
    /// policy, a filter that reads what it adds here.
    /// </remarks>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets what is said about the tool.
    /// </summary>
    /// <remarks>
    /// Untyped, because a host says things this library has never heard of. It ends up as
    /// <see cref="AgentToolDescriptor.Metadata"/>.
    /// </remarks>
    IList<object> Metadata { get; }
}
