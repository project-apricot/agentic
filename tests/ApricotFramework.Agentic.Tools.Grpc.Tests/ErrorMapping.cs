namespace ApricotFramework.Agentic.Tools.Grpc.Tests;

/// <summary>
/// Where, if anywhere, the host translates transport failures into its own error type.
/// </summary>
public enum ErrorMapping
{
    /// <summary>Nowhere; the call fails with the transport's own exception.</summary>
    None,

    /// <summary>On this client's registration.</summary>
    OnTheClient,

    /// <summary>For every client in the container, without this one's registration saying so.</summary>
    ContainerWide
}
