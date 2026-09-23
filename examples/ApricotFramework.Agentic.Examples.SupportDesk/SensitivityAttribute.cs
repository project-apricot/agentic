using ApricotFramework.Agentic.Tools;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// How sensitive the data a tool exposes is.
/// </summary>
/// <remarks>
/// <para>
/// A label, closed at the point of declaration. Deriving
/// <see cref="AgentToolLabelAttribute"/> rather than writing an unrelated attribute is what makes
/// it readable both ways: <see cref="Validators.SensitivityDeclaredValidator"/> and any portable
/// filter read it as the <c>sensitivity</c> label without knowing this type exists, while a
/// handler of this application's own can read it out of the metadata as itself.
/// </para>
/// <para>
/// What it buys over writing the label by hand: no name to mistype, no value that is not one of
/// the enum's, and <c>AllowMultiple = false</c> saying that one per tool is what is meant.
/// </para>
/// </remarks>
/// <param name="level">How sensitive the data is.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SensitivityAttribute(Sensitivity level) : AgentToolLabelAttribute(SupportDeskLabels.Sensitivity, level)
{
    /// <summary>
    /// Gets how sensitive the data is.
    /// </summary>
    public Sensitivity Level { get; } = level;
}
