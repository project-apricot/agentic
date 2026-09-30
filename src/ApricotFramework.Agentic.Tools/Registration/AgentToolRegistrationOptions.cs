namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// The tools a host registered in code.
/// </summary>
/// <remarks>
/// Each entry is already finished: whatever was said about the tool was said inside the
/// <c>AddAgentTool</c> call that added it, so there is nothing handed back to be mutated
/// afterward and nothing deferred except building the tool itself, which needs a container that
/// does not exist yet.
/// </remarks>
public sealed class AgentToolRegistrationOptions
{
    /// <summary>
    /// Gets how to describe each registered tool, once there is a container.
    /// </summary>
    public IList<Func<IServiceProvider, AgentToolDescriptor>> Registrations { get; } = [];
}
