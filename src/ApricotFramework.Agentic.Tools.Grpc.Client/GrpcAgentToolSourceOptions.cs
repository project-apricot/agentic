namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// Options for offering another service's tools here.
/// </summary>
public sealed class GrpcAgentToolSourceOptions
{
    /// <summary>
    /// Gets or sets the prefix for every remote tool's name, or null for none.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets how long a listing is cached; zero, the default, disables caching.
    /// </summary>
    /// <remarks>
    /// The serving host filters its listing by caller, so only cache when the remote surface is safe to share per <see cref="CacheKey"/>.
    /// </remarks>
    public TimeSpan Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the cache key for a listing, or null to key it by the caller's subject.
    /// </summary>
    /// <remarks>
    /// Only read when <see cref="Lifetime"/> is positive. The default keys by the <c>sub</c> or
    /// name-identifier claim, or identity name; calls with no caller share one entry. Returning null
    /// leaves the call uncached.
    /// </remarks>
    public Func<IAgentToolSourceContext, string?>? CacheKey { get; set; }

    /// <summary>
    /// Gets or sets the deadline for invoking a tool, or null to use the client's registration.
    /// </summary>
    /// <remarks>
    /// Applies to invocation only, not listing.
    /// </remarks>
    public TimeSpan? CallDeadline { get; set; }

    /// <summary>
    /// Gets or sets whether this service's tools must load for the listing to succeed.
    /// </summary>
    /// <remarks>
    /// False by default: an unreachable service, a tool refused by validation, or a name collision
    /// drops that service's tools with a log entry. True fails the listing instead.
    /// </remarks>
    public bool RequireService { get; set; }

    /// <summary>
    /// Gets metadata attached to every tool read off the service.
    /// </summary>
    /// <remarks>
    /// Remote tools carry no authorization attributes; add local gating here.
    /// </remarks>
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Gets or sets a final transform for each tool before it is offered, or null for none.
    /// </summary>
    /// <remarks>
    /// Returning null leaves a tool out.
    /// </remarks>
    public Func<AgentToolDescriptor, AgentToolDescriptor?>? Curate { get; set; }
}
