using ApricotFramework.Agentic.Tools.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Describes tools implemented as classes.
/// </summary>
internal static class AgentToolTypes
{
    /// <summary>
    /// Describes a tool type as a tool resolved per call.
    /// </summary>
    /// <param name="services">A scope to read the declaration in.</param>
    /// <param name="toolType">The tool's type.</param>
    /// <param name="metadata">Host-supplied metadata.</param>
    /// <returns>The descriptor.</returns>
    /// <remarks>
    /// One instance is built here only to read its declaration, which must be the same for every caller.
    /// </remarks>
    internal static AgentToolDescriptor Describe(IServiceProvider services, Type toolType, IReadOnlyList<object> metadata)
    {
        var snapshot = (AgentTool)ActivatorUtilities.GetServiceOrCreateInstance(services, toolType);

        var tool = new ScopedAgentTool(toolType, snapshot);

        return new AgentToolDescriptor(tool, tool, metadata);
    }
}
