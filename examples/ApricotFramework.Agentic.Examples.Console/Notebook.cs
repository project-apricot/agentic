namespace ApricotFramework.Agentic.Examples.Console;

/// <summary>
/// A scoped thing a tool depends on, so the scope is visible in the output.
/// </summary>
/// <remarks>
/// Registered as scoped. The executor opens a scope per call, so each call gets its own - which
/// is the property the whole hosting layer exists for, and the one that used to be broken.
/// </remarks>
public sealed class Notebook
{
    /// <summary>
    /// How many have been built.
    /// </summary>
    private static int built;

    /// <summary>
    /// Gets which one this is.
    /// </summary>
    public int Ordinal { get; } = Interlocked.Increment(ref built);

    /// <summary>
    /// The notes taken in this call.
    /// </summary>
    private readonly List<string> notes = [];

    /// <summary>
    /// Takes a note.
    /// </summary>
    /// <param name="note">What to write down.</param>
    /// <returns>How many notes this call has taken.</returns>
    public int Write(string note)
    {
        this.notes.Add(note);

        return this.notes.Count;
    }
}
