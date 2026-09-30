using ApricotFramework.Agentic.Tools.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.Agentic.Tools.Registration;

/// <summary>
/// Describing a tool that is a class.
/// </summary>
internal static class AgentToolTypes
{
    /// <summary>
    /// Reads what a tool type declares, and returns it as something resolved per call.
    /// </summary>
    /// <param name="services">A scope to read the declaration in.</param>
    /// <param name="toolType">The tool's type.</param>
    /// <param name="metadata">What a host said about it.</param>
    /// <returns>The descriptor.</returns>
    /// <remarks>
    /// One instance is built here and read, never run. What it says - a name, a title, prose,
    /// labels, two schemas - is the same for every caller, so reading it once is not a shortcut.
    /// </remarks>
    internal static AgentToolDescriptor Describe(IServiceProvider services, Type toolType, IReadOnlyList<object> metadata)
    {
        var snapshot = (AgentTool)ActivatorUtilities.GetServiceOrCreateInstance(services, toolType);

        var tool = new ScopedAgentTool(toolType, snapshot);

        return new AgentToolDescriptor(tool, tool, metadata);
    }
}
