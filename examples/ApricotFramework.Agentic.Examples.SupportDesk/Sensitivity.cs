namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// How sensitive the data a tool exposes is.
/// </summary>
/// <remarks>
/// A vocabulary this application defines, carried as an <c>[AgentToolLabel]</c>. Nothing in the
/// library knows it exists - which is the point of labels being open. A policy, a validator or a
/// surface can read it; see <see cref="Validators.SensitivityDeclaredValidator"/> for closing the
/// vocabulary so a tool cannot forget to state one.
/// </remarks>
public enum Sensitivity
{
    /// <summary>Nothing a stranger could not see.</summary>
    Public = 0,

    /// <summary>Internal business data.</summary>
    Internal = 1,

    /// <summary>Data about an identifiable person.</summary>
    Personal = 2
}
