namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The labels this application puts on its tools.
/// </summary>
/// <remarks>
/// All of it is ours. The library has no notion of a surface, a product area or a sensitivity - it
/// carries labels and lets a filter or a validator read them, which is what keeps a vocabulary
/// this specific out of a package everyone shares.
/// </remarks>
public static class SupportDeskLabels
{
    /// <summary>How sensitive the data a tool exposes is.</summary>
    public const string Sensitivity = "sensitivity";

    /// <summary>The product area a tool belongs to.</summary>
    public const string Area = "area";

    /// <summary>The surfaces a tool may be offered on, as a comma separated list.</summary>
    /// <remarks>
    /// A label rather than a property on the tool, because which surfaces exist is this
    /// application's business. <see cref="Filters.SurfaceAgentToolFilter"/> is what acts on it,
    /// and registering that filter is what makes it apply.
    /// </remarks>
    public const string Surfaces = "surfaces";
}
