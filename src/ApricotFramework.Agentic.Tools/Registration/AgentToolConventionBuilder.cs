using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// The <see cref="IAgentToolConventionBuilder"/> handed to registration callbacks.
/// </summary>
/// <param name="services">The service collection.</param>
internal sealed class AgentToolConventionBuilder(IServiceCollection services) : IAgentToolConventionBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc />
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Seeds the metadata from the attributes on a type, including inherited ones.
    /// </summary>
    /// <param name="toolType">The tool's type.</param>
    /// <returns>The same builder.</returns>
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
    /// Seeds the metadata from the attributes on a method and its declaring type.
    /// </summary>
    /// <param name="method">The method behind the tool.</param>
    /// <returns>The same builder.</returns>
    /// <remarks>Class attributes are added first, so method attributes override them.</remarks>
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
    /// Runs the host's callback and returns the final metadata.
    /// </summary>
    /// <param name="configure">The host's callback, or null.</param>
    /// <returns>The metadata.</returns>
    internal IReadOnlyList<object> Build(Action<IAgentToolConventionBuilder>? configure)
    {
        configure?.Invoke(this);

        return [.. this.Metadata];
    }
}
