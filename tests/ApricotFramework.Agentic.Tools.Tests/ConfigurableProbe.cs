namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A tool whose declaration is shaped by the test.</summary>
/// <param name="name">The name to declare.</param>
/// <param name="title">The title to declare.</param>
/// <param name="description">The description to declare.</param>
/// <param name="readOnly">Whether to declare read only.</param>
/// <param name="destructive">Whether to declare destructive.</param>
public sealed class ConfigurableProbe(
    string name,
    string title = "Probe",
    string description = "Does a thing.",
    bool readOnly = true,
    bool destructive = false) : AgentTool<AgentToolNoArguments, string>
{
    /// <inheritdoc />
    public override string Name => name;

    /// <inheritdoc />
    public override string Title => title;

    /// <inheritdoc />
    public override string Description => description;

    /// <inheritdoc />
    public override bool IsReadOnly => readOnly;

    /// <inheritdoc />
    public override bool IsDestructive => destructive;


    /// <inheritdoc />
    protected override Task<string> ExecuteAsync(AgentToolNoArguments arguments, AgentToolContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult("ok");
    }
}
