using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// What a service currently hides or refuses, changeable while it runs.
/// </summary>
/// <remarks>
/// So a test can list a tool, take it away on the serving side, and see how the call comes back.
/// </remarks>
public sealed class Gate
{
    /// <summary>
    /// The tools no caller is offered.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> hidden = new(StringComparer.Ordinal);

    /// <summary>
    /// The tools every caller is refused.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> refused = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, bool> unauthenticated = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether a tool is hidden.
    /// </summary>
    /// <param name="name">The tool.</param>
    /// <returns>Whether it is.</returns>
    public bool Hides(string name) => this.hidden.ContainsKey(name);

    /// <summary>
    /// Gets whether a tool is refused.
    /// </summary>
    /// <param name="name">The tool.</param>
    /// <returns>Whether it is.</returns>
    public bool Refuses(string name) => this.refused.ContainsKey(name);

    public bool WantsACaller(string name) => this.unauthenticated.ContainsKey(name);

    /// <summary>
    /// Hides a tool.
    /// </summary>
    /// <param name="name">The tool.</param>
    public void Hide(string name) => this.hidden[name] = true;

    /// <summary>
    /// Refuses a tool.
    /// </summary>
    /// <param name="name">The tool.</param>
    public void Refuse(string name) => this.refused[name] = true;

    public void RequireACaller(string name) => this.unauthenticated[name] = true;

    /// <summary>
    /// Opens everything again.
    /// </summary>
    public void Reset()
    {
        this.hidden.Clear();
        this.refused.Clear();
        this.unauthenticated.Clear();
    }
}
