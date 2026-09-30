using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Validators;

/// <summary>
/// Insists every tool says how sensitive its data is.
/// </summary>
/// <remarks>
/// <para>
/// The other half of an open label vocabulary. Labels are strings so the library does not have to
/// know what <see cref="Sensitivity"/> means; a validator is how an application closes the set, so
/// a tool cannot ship without stating one and cannot state something nobody recognises.
/// </para>
/// <para>
/// Closing it at startup is the point. A label nobody notices missing is a policy that quietly
/// stops applying, where a refusal here names the tool to whoever is adding it.
/// </para>
/// </remarks>
public sealed class SensitivityDeclaredValidator : IAgentToolValidator
{
    /// <inheritdoc />
    public void Validate(AgentToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (!tool.Declaration.TryGetLabel<Sensitivity>(SupportDeskLabels.Sensitivity, out var sensitivity))
        {
            throw new AgentToolDeclarationException(
                $"The tool '{tool.Name}' declares no '{SupportDeskLabels.Sensitivity}' label. " +
                $"Add [AgentToolLabel(\"{SupportDeskLabels.Sensitivity}\", Sensitivity.…)].");
        }

        if (!Enum.IsDefined(sensitivity))
        {
            throw new AgentToolDeclarationException($"The tool '{tool.Name}' declares a sensitivity of '{sensitivity}', which is not one this application recognises.");
        }
    }
}
