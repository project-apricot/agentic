namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// The tools a host registered in code.
/// </summary>
/// <remarks>Descriptors are built later, once the container exists.</remarks>
public sealed class AgentToolRegistrationOptions
{
    /// <summary>
    /// Gets the descriptor factories, one per registered tool.
    /// </summary>
    public IList<Func<IServiceProvider, AgentToolDescriptor>> Registrations { get; } = [];
}
