namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// How far along a tool is.
/// </summary>
/// <remarks>
/// For the person watching, not for the model. A tool reporting progress is not reporting results:
/// a caller assembling a result still receives it whole, because a model handed part of an answer
/// has no way to tell that is what happened.
/// </remarks>
public sealed class AgentToolProgress
{
    /// <summary>
    /// The message to report
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// How much is done in the given unit
    /// </summary>
    public double? Completed { get; set; }

    /// <summary>
    /// The total amount of work to be done
    /// </summary>
    public double? Total { get; set; }

    /// <summary>
    /// Build the progress instance from the given arguments
    /// </summary>
    /// <param name="message">The message to report</param>
    /// <param name="completed">How much work is completed</param>
    /// <param name="total">The total amount of work</param>
    /// <returns></returns>
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
