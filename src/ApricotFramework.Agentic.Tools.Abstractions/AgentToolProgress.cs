namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Progress reported by a tool.
/// </summary>
/// <remarks>
/// For the person watching, not the model; results are still delivered whole.
/// </remarks>
public sealed class AgentToolProgress
{
    /// <summary>
    /// The progress message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The amount done.
    /// </summary>
    public double? Completed { get; set; }

    /// <summary>
    /// The total amount of work.
    /// </summary>
    public double? Total { get; set; }

    /// <summary>
    /// Creates a progress instance.
    /// </summary>
    /// <param name="message">The progress message.</param>
    /// <param name="completed">The amount done.</param>
    /// <param name="total">The total amount of work.</param>
    /// <returns>The progress.</returns>
    public static AgentToolProgress From(string? message = null, double? completed = null, double? total = null)
    {
        return new AgentToolProgress
        {
            Message = message,
            Completed = completed,
            Total = total
        };
    }
}
