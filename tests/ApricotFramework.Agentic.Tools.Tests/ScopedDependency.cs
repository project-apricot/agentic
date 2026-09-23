namespace ApricotFramework.Agentic.Tools.Tests;

/// <summary>
/// A dependency registered as scoped, so that two calls see two of them.
/// </summary>
/// <remarks>
/// The whole point of resolving a tool per call. Under the previous design this was built once
/// from the root container and every caller shared it.
/// </remarks>
public sealed class ScopedDependency : IDisposable
{
    /// <summary>
    /// How many have been built.
    /// </summary>
    private static int built;

    /// <summary>
    /// Gets how many have been disposed.
    /// </summary>
    public static int Disposed { get; private set; }

    /// <summary>
    /// Gets which one this is.
    /// </summary>
    public int Ordinal { get; } = Interlocked.Increment(ref built);

    /// <summary>
    /// Forgets what has been counted.
    /// </summary>
    public static void Reset()
    {
        built = 0;
        Disposed = 0;
    }

    /// <inheritdoc />
    public void Dispose() => Disposed++;
}
