namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// The surfaces this example serves.
/// </summary>
public static class SupportDeskSurfaces
{
    /// <summary>Third party agents, over the Model Context Protocol.</summary>
    public const string Mcp = "mcp";

    /// <summary>The support desk's own agent loop, inside the application.</summary>
    public const string Internal = "internal";

    /// <summary>Both of them, which is where most tools belong.</summary>
    public const string Both = $"{Mcp},{Internal}";
}
