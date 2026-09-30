namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// The two ways a host can say which client reaches a service.
/// </summary>
public enum ClientRegistration
{
    /// <summary>A type per service.</summary>
    Typed,

    /// <summary>The generated client, under a name per service.</summary>
    Named
}
