namespace ApricotFramework.Agentic.Tools.Adapters;

/// <summary>
/// A tool offered under a different name.
/// </summary>
/// <remarks>
/// <para>
/// The commonest thing a host needs to change about a tool it inherited. A foreign server's
/// <c>search</c> will collide with something, or shadow it, and the server had no way to know -
/// so the name it chose is the host's problem to solve rather than the server's.
/// </para>
/// <para>
/// Only the name changes. <see cref="AgentTool.Title"/> is a label a person reads and stays
/// whatever the tool called itself.
/// </para>
/// </remarks>
/// <param name="inner">The tool to offer.</param>
/// <param name="name">The name to offer it under.</param>
public sealed class RenamedAgentTool(AgentTool inner, string name) : DelegatingAgentTool(inner)
{
    /// <inheritdoc />
    public override string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("A tool needs a name to be offered under.", nameof(name))
        : name;
}
