using ApricotFramework.Agentic.Tools.Exceptions;

namespace ApricotFramework.Agentic.Tools.Sources;

/// <summary>
/// Common curators for <see cref="CuratingAgentToolSource"/>.
/// </summary>
/// <remarks>Each returns a function, so they compose.</remarks>
public static class AgentToolCuration
{
    /// <summary>
    /// Prefixes every tool's name.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> is null or blank.</exception>
    /// <remarks>Avoids name clashes, which the registry rejects. The function is not wrapped.</remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Prefixing(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return tool => tool.With(AgentToolDeclaration.From(tool.Declaration) with { Name = prefix + tool.Name });
    }

    /// <summary>
    /// Adds metadata to every tool.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
    /// <remarks>How external tools get authorization metadata; pass what <c>RequireAuthorization</c> would add.</remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Adding(params object[] metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return tool => tool.With(tool.Declaration, [.. tool.Metadata, .. metadata]);
    }

    /// <summary>
    /// Adds metadata to each tool, computed from the tool.
    /// </summary>
    /// <param name="metadata">The metadata for a tool, appended to its existing metadata.</param>
    /// <returns>The curation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
    /// <remarks>Keeps existing metadata, unlike <see cref="AgentToolDescriptor.With"/>.</remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> AddingFor(Func<AgentToolDescriptor, IEnumerable<object>> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return tool => tool.WithMetadata([.. metadata(tool)]);
    }

    /// <summary>
    /// Keeps only tools matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    /// <remarks>Typically an allowlist, so tools added upstream later are not offered unreviewed.</remarks>
    public static Func<AgentToolDescriptor, AgentToolDescriptor?> Where(Func<AgentToolDescriptor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return tool => predicate(tool) ? tool : null;
    }

    /// <summary>
    /// Drops tools that validation would reject.
    /// </summary>
    /// <param name="validators">The validators; should match the registry's.</param>
    /// <param name="onRejected">Called for each dropped tool and reason; log it.</param>
    /// <returns>The curator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validators"/> is null.</exception>
    /// <remarks>Cannot detect duplicate names, which need the full listing; use <see cref="Prefixing"/> to avoid those.</remarks>
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
