namespace ApricotFramework.Agentic.Tools.Grpc.Client;

/// <summary>
/// How another service's tools are offered here.
/// </summary>
public sealed class GrpcAgentToolSourceOptions
{
    /// <summary>
    /// Gets or sets what is put in front of every remote tool's name, or null for nothing.
    /// </summary>
    /// <remarks>
    /// Null is the usual answer here, unlike for a foreign MCP server. Services in one fleet
    /// already name their tools by domain, and prefixing again would say it twice.
    /// </remarks>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets how long a listing is held before the service is asked again.
    /// </summary>
    /// <remarks>
    /// Zero by default, meaning no caching, and that is deliberate. The serving host filters its
    /// listing by who is asking, so one caller's answer is not another's - a host whose remote
    /// surface is the same for everybody can safely set this, and one that is not should leave
    /// it alone.
    /// </remarks>
    public TimeSpan Lifetime { get; set; }

    /// <summary>
    /// Gets what is said about every tool read off the service.
    /// </summary>
    /// <remarks>
    /// Nothing that arrives over the wire carries an authorization attribute. Where this host
    /// gates a remote tool on top of whatever the serving host does, this is where it says so.
    /// </remarks>
    public IList<object> Metadata { get; } = [];

    /// <summary>
    /// Gets or sets a last look at each tool before it is offered, or null for none.
    /// </summary>
    /// <remarks>
    /// Returning null leaves a tool out, which is how an allowlist is written.
    /// </remarks>
    public Func<AgentToolDescriptor, AgentToolDescriptor?>? Curate { get; set; }
}
