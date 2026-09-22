namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Somewhere tools come from.
/// </summary>
/// <remarks>
/// <para>
/// The seam that keeps the registry from assuming every tool is a class the host wrote. Tools
/// declared in code are one source; tools adapted from a function, built from configuration, or
/// read off a foreign server the host happens to speak to are others, and the registry composes
/// them without knowing which is which.
/// </para>
/// <para>
/// A source hands over <see cref="AgentToolDescriptor"/> rather than bare tools, so it says what
/// it wants to be said about them. That is what a foreign source needs: nothing it fetches carries an
/// authorization attribute, so whatever gates it has to be attached by whoever fetched it.
/// </para>
/// <para>
/// Asynchronous because some of those cannot be enumerated while a container is being built - a
/// connection has to be made first, and a connection is not something to make inside
/// <c>ConfigureServices</c>.
/// </para>
/// <para>
/// A source that can fail is expected to decide for itself what a failure means. Tools the host
/// wrote should stop it from starting; tools from somewhere it does not control should be logged
/// and left out, because a service that will not start over a third party's mistake is worse than
/// one that runs with fewer tools. See <c>CuratingAgentToolSource</c>.
/// </para>
/// </remarks>
public interface IAgentToolSource
{
    /// <summary>
    /// Gets the tools this source offers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the tools.</returns>
    ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default);
}
