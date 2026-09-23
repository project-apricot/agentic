using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// What a registration callback writes into.
/// </summary>
/// <param name="services">The collection the tool is being registered into.</param>
internal sealed class AgentToolConventionBuilder(IServiceCollection services) : IAgentToolConventionBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc />
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Seeds the metadata from the attributes on a type.
    /// </summary>
    /// <param name="toolType">The tool's type.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// Inherited, so an attribute on a base class reaches every tool deriving from it - which is
    /// how a family of tools declares a shared requirement once.
    /// </remarks>
    internal AgentToolConventionBuilder WithAttributesOf(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);

        foreach (var attribute in toolType.GetCustomAttributes(inherit: true))
        {
            this.Metadata.Add(attribute);
        }

        return this;
    }

    /// <summary>
    /// Seeds the metadata from the attributes on a method and the class it is declared in.
    /// </summary>
    /// <param name="method">The method behind the tool.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// The class first, so a method restating something overrides its family rather than being
    /// overridden by it - the order metadata is read in is the order it was added.
    /// </remarks>
    internal AgentToolConventionBuilder WithAttributesOf(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (method.DeclaringType is { } declaring)
        {
            this.WithAttributesOf(declaring);
        }

        foreach (var attribute in method.GetCustomAttributes(inherit: true))
        {
            this.Metadata.Add(attribute);
        }

        return this;
    }

    /// <summary>
    /// Runs the host's callback and settles what was said.
    /// </summary>
    /// <param name="configure">What the host wants to say, or null for nothing.</param>
    /// <returns>What was said, as it will be read.</returns>
    internal IReadOnlyList<object> Build(Action<IAgentToolConventionBuilder>? configure)
    {
        configure?.Invoke(this);

        return [.. this.Metadata];
    }
}
