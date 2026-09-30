namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// How this host decides who is asking.
/// </summary>
/// <remarks>
/// <para>
/// One host, one answer. An HTTP host reads the request's principal and its services; an MCP
/// server reads the session's; a console app has no caller at all and says so. Registering one
/// of these is what lets every surface call the executor without building a context of its own.
/// </para>
/// <para>
/// A host with state worth naming derives from <see cref="AgentToolContext"/> and returns that,
/// which a tool or a source of the host's own can cast to.
/// </para>
/// </remarks>
public interface IAgentToolContextFactory
{
    /// <summary>
    /// Builds the context for one call.
    /// </summary>
    /// <param name="scopedServices">The scope opened for this call.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the context.</returns>
    ValueTask<AgentToolContext> CreateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken = default);
}
