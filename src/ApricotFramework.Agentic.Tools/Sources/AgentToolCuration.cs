using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// The curators a host is most likely to want, for use with <see cref="CuratingAgentToolSource"/>.
/// </summary>
/// <remarks>
/// Each returns a function rather than a source, so they nest: a host prefixes a foreign server's
/// names, attaches what should gate them, then drops whatever still would not pass, and the three
/// are separate decisions made in a readable order.
/// </remarks>
public static class AgentToolCuration
{
    /// <summary>
    /// Offers every tool under a prefixed name.
    /// </summary>
    /// <param name="prefix">What to put in front of each name.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null or blank.</exception>
    /// <remarks>
    /// <para>
    /// The answer to a foreign server whose names were chosen without knowing what they would sit
    /// beside. A collision is genuinely ambiguous, and the registry refuses it rather than picking
    /// one, so putting the names in a space of their own is what keeps that from happening.
    /// </para>
    /// <para>
    /// Nothing wraps the function. A prefixed tool is the same function under a different
    /// declaration, so a consumer reaching through it for what it really is still finds it.
    /// </para>
    /// </remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Prefixing(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return tool => tool.With(AgentToolDeclaration.From(tool.Declaration) with { Name = prefix + tool.Name });
    }

    /// <summary>
    /// Adds to what was said about every tool.
    /// </summary>
    /// <param name="metadata">What to add.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadata"/> is null.</exception>
    /// <remarks>
    /// How a foreign tool gets gated. Nothing a host fetches carries an authorization attribute,
    /// so whatever should govern it is attached here - pass the same attributes
    /// <c>RequireAuthorization</c> would have added.
    /// </remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Adding(params object[] metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return tool => tool.With(tool.Declaration, [.. tool.Metadata, .. metadata]);
    }

    /// <summary>
    /// Offers only the tools passing a test.
    /// </summary>
    /// <param name="predicate">The test each tool has to pass.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    /// <remarks>
    /// An allowlist, most usefully. A foreign server can add a tool after a host has looked at
    /// what it offers, and naming the ones that were reviewed is the only way that stays true.
    /// </remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Where(Func<AgentToolDescriptor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return tool => predicate(tool) ? tool : null;
    }

    /// <summary>
    /// Leaves out the tools validation would refuse.
    /// </summary>
    /// <param name="validators">The checks to apply, which should be the ones the registry applies.</param>
    /// <param name="onRejected">Told about each tool left out and why. Worth logging.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validators"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// So that one malformed declaration from somewhere a host does not control costs it that tool
    /// rather than all of them. The registry refuses a tool it cannot accept, which is right for
    /// tools a host wrote - a host should not ship one it got wrong - and wrong for tools it
    /// merely reached.
    /// </para>
    /// <para>
    /// It cannot catch everything, and the exception is worth knowing: a name two tools share is
    /// only visible once both are in front of the registry, so no per-tool check will find it.
    /// <see cref="Prefixing"/> is how that one is avoided rather than detected.
    /// </para>
    /// <para>
    /// Report what it drops somewhere a person will see. A tool silently absent is the failure
    /// nobody can diagnose, and this turns a loud one into a quiet one on purpose.
    /// </para>
    /// </remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> DropRejected(
        IEnumerable<IAgentToolValidator> validators,
        Action<AgentToolDescriptor, AgentToolDeclarationException>? onRejected = null)
    {
        ArgumentNullException.ThrowIfNull(validators);

        var checks = validators.ToList();

        return tool =>
        {
            foreach (var check in checks)
            {
                try
                {
                    check.Validate(tool);
                }
                catch (AgentToolDeclarationException exception)
                {
                    onRejected?.Invoke(tool, exception);

                    return null;
                }
            }

            return tool;
        };
    }
}
