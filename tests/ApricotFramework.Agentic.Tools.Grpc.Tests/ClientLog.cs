using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// Every client the factory made, which service it reached and which scope it was made in.
/// </summary>
public sealed class ClientLog
{
    /// <summary>
    /// What has been made.
    /// </summary>
    private readonly ConcurrentQueue<(string Service, Guid Scope)> made = new();

    /// <summary>
    /// Gets what has been made, in order.
    /// </summary>
    public IReadOnlyList<(string Service, Guid Scope)> Made => [.. this.made];

    /// <summary>
    /// Counts the clients made for one service.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <returns>How many.</returns>
    public int For(string service) => this.made.Count(entry => entry.Service == service);

    /// <summary>
    /// Records a client.
    /// </summary>
    /// <param name="service">The service it reaches.</param>
    /// <param name="scope">The scope it was made in.</param>
    public void Record(string service, Guid scope) => this.made.Enqueue((service, scope));
}
