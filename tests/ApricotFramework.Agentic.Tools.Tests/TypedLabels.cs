using System.ComponentModel;

namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>A vocabulary a host closes for itself.</summary>
public enum Confidentiality
{
    /// <summary>Safe to say anywhere.</summary>
    Public,

    /// <summary>About a person.</summary>
    Personal
}

/// <summary>The names a host's own labels go under.</summary>
public static class TypedLabelNames
{
    /// <summary>How confidential a tool's data is.</summary>
    public const string Confidentiality = "confidentiality";
}

/// <summary>
/// A host's own label, typed.
/// </summary>
/// <remarks>
/// Deriving <see cref="AgentToolLabelAttribute"/> rather than writing an unrelated attribute is
/// what makes it both: a portable filter reads it as a label, and the host's own handler reads it
/// as itself out of the metadata.
/// </remarks>
/// <param name="level">How confidential the tool's data is.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ConfidentialityAttribute(Confidentiality level) : AgentToolLabelAttribute(TypedLabelNames.Confidentiality, level)
{
    /// <summary>Gets how confidential the tool's data is.</summary>
    public Confidentiality Level { get; } = level;
}

/// <summary>Two tools carrying a typed label, one narrowing its family's.</summary>
[AgentToolType]
[Confidentiality(Confidentiality.Public)]
public sealed class TypedLabelProbes
{
    /// <summary>Takes the family's label.</summary>
    /// <returns>Something.</returns>
    [AgentTool("probe_typed_family", ReadOnly = true)]
    [Description("Inherits the family's label.")]
    public static string Family() => "ok";

    /// <summary>Narrows it.</summary>
    /// <returns>Something.</returns>
    [AgentTool("probe_typed_narrowed", ReadOnly = true)]
    [Confidentiality(Confidentiality.Personal)]
    [Description("Narrows the family's label.")]
    public static string Narrowed() => "ok";
}
