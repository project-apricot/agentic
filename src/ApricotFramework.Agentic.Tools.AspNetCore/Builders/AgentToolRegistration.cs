namespace ApricotFramework.Agentic.Tools.AspNetCore.Builders;

/// <summary>
/// One tool being registered: how to get it, and what is being said about it.
/// </summary>
/// <remarks>
/// <para>
/// A registration rather than a <see cref="AgentToolDescriptor"/> because neither half of a
/// descriptor is available yet. The tool itself may be resolved from the container, which does not
/// exist while <c>AddAgentTool</c> runs; and the metadata is still open, because
/// <c>RequireAuthorization</c> is a separate call that has to reach the same tool afterwards. A
/// descriptor is immutable and holds an instance, so it can only be the product.
/// </para>
/// <para>
/// The same split ASP.NET Core makes between <c>EndpointBuilder</c> and <c>Endpoint</c>, for the
/// same two reasons, which is why this is the thing the service collection carries and the
/// descriptor is what the source builds from it - once, as the registry composes.
/// </para>
/// <para>
/// Metadata starts as the attributes on the tool's type, the same way ASP.NET Core seeds an
/// endpoint from the attributes on a controller and its action. A <c>Require*</c> call adds to
/// that rather than replacing it, which is why a tool carrying an attribute and registered with a
/// policy has to satisfy both.
/// </para>
/// </remarks>
/// <param name="factory">How to get the tool.</param>
public sealed class AgentToolRegistration(Func<IServiceProvider, AgentTool> factory) : IAgentToolBuilder
{
    /// <summary>
    /// Gets how to get the tool.
    /// </summary>
    public Func<IServiceProvider, AgentTool> Factory { get; } = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <inheritdoc />
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Seeds the metadata from the attributes on a type.
    /// </summary>
    /// <param name="toolType">The tool's type.</param>
    /// <returns>The same registration for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toolType"/> is null.</exception>
    /// <remarks>
    /// Inherited, so an attribute on a base class reaches every tool deriving from it - which is
    /// how a family of tools declares a shared requirement once.
    /// </remarks>
    public AgentToolRegistration WithAttributesOf(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);

        foreach (var attribute in toolType.GetCustomAttributes(inherit: true))
        {
            this.Metadata.Add(attribute);
        }

        return this;
    }

    /// <summary>
    /// Builds the descriptor.
    /// </summary>
    /// <param name="services">Where the tool's own dependencies come from.</param>
    /// <returns>The descriptor.</returns>
    public AgentToolDescriptor Build(IServiceProvider services) => new(this.Factory(services), this.Metadata);
}
