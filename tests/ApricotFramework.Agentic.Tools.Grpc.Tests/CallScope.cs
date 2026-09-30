namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// Registered scoped, so that a client made in a call's scope can say which call it was.
/// </summary>
public sealed class CallScope
{
    /// <summary>
    /// Gets which scope this is.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
