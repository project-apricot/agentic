using ApricotFramework.Agentic.Tools;
using System.ComponentModel;

namespace ApricotFramework.Agentic.Examples.Console.Tools;

/// <summary>
/// Two tools written as methods.
/// </summary>
/// <param name="notebook">Something scoped, resolved again for every call.</param>
[AgentToolType]
public sealed class NotesTools(Notebook notebook)
{
    /// <summary>Writes a note down.</summary>
    /// <param name="note">What to write.</param>
    /// <returns>What was written and how many notes this call holds.</returns>
    [AgentTool("notes_write", Title = "Write a note", Destructive = false)]
    [Description("Writes a note down and reports how many notes the current call is holding.")]
    public object Write([Description("The note to write down.")] string note) =>
        new { Note = note, Count = notebook.Write(note), Scope = notebook.Ordinal };

    /// <summary>Reports which scope this call ran in.</summary>
    /// <returns>The scope's ordinal.</returns>
    [AgentTool("notes_scope", Title = "Report the scope", ReadOnly = true)]
    [Description("Reports which scope this call ran in. Two calls report two different scopes.")]
    public object Scope() => new { Scope = notebook.Ordinal };

    /// <summary>Says a message back.</summary>
    /// <param name="message">What to say back.</param>
    /// <param name="context">Who is asking.</param>
    /// <returns>What was said, and who asked.</returns>
    /// <remarks>
    /// Takes the invocation as a parameter. It is bound from the call and left out of the schema,
    /// so a model is never asked to fill in who is calling.
    /// </remarks>
    [AgentTool("notes_echo", Title = "Echo", ReadOnly = true)]
    [Description("Says a message back, reporting which scope the call ran in and who asked.")]
    public object Echo(
        [Description("What to say back.")] string message,
        AgentToolContext context) =>
        new { Echoed = message, Scope = notebook.Ordinal, Caller = context.User?.Identity?.Name ?? "nobody" };
}
