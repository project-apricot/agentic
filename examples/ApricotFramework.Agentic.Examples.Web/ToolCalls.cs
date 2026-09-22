using System.Text.Json;
using ApricotFramework.Agentic.Examples.SupportDesk;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Serialization;

namespace ApricotFramework.Agentic.Examples.Web;

/// <summary>
/// What the endpoints share.
/// </summary>
public static class ToolCalls
{
    /// <summary>
    /// Builds the context describing who is asking.
    /// </summary>
    /// <param name="http">The request.</param>
    /// <param name="surface">The surface the caller claims to be on, or null for none.</param>
    /// <returns>The context.</returns>
    /// <remarks>
    /// The caller, not the call - which tool and what arguments are parameters. A loop putting
    /// several questions to an agent on one person's behalf would build this once.
    /// </remarks>
    /// <remarks>
    /// A <see cref="SupportDeskAgentToolContext"/> rather than the base type, because the surface
    /// is this application's own idea and the library carries nothing for it.
    /// </remarks>
    public static AgentToolContext Caller(HttpContext http, string? surface) => new SupportDeskAgentToolContext
    {
        User = http.User.Identity?.IsAuthenticated == true ? http.User : null,
        Services = http.RequestServices,
        Surface = surface
    };

    /// <summary>
    /// Reads the request body as the tool's arguments.
    /// </summary>
    /// <param name="http">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the arguments, or null where the body was empty.</returns>
    public static async Task<string?> ReadArgumentsAsync(HttpContext http, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(http.Request.Body);

        var body = await reader.ReadToEndAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    /// <summary>
    /// Describes a tool the way a listing would.
    /// </summary>
    /// <param name="tool">The tool to describe.</param>
    /// <param name="schemas">Whether to include the schemas.</param>
    /// <returns>The description.</returns>
    public static object Describe(AgentTool tool, bool schemas = false)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return new
        {
            tool.Name,
            tool.Title,
            tool.Description,
            tool.IsReadOnly,
            tool.IsDestructive,
            tool.IsIdempotent,
            tool.IsOpenWorld,
            ResultKind = tool.ResultKind.ToString(),
            Labels = tool.Labels.ToDictionary(label => label.Key, label => label.Value?.ToString()),
            InputSchema = schemas ? (JsonElement?)tool.InputSchema : null,
            OutputSchema = schemas ? tool.OutputSchema : null
        };
    }

    /// <summary>
    /// Describes a tool the way a listing would.
    /// </summary>
    /// <param name="tool">The tool to describe.</param>
    /// <returns>The description.</returns>
    public static object Describe(AgentTool tool) => Describe(tool, schemas: true);

    /// <summary>
    /// Puts a result on one line.
    /// </summary>
    /// <param name="json">The result as the invoker wrote it.</param>
    /// <returns>The same JSON, without the whitespace.</returns>
    /// <remarks>
    /// The library writes results indented, because its serializer options are the AI
    /// abstractions' own and a model reads an indented result more reliably. A line delimited
    /// stream wants the opposite, so the surface compacts rather than the tools changing what they
    /// produce - a tool overriding <c>SerializerOptions</c> to suit one transport would be wrong
    /// for the next one.
    /// </remarks>
    public static string OneLine(string json)
    {
        using var document = JsonDocument.Parse(json);

        return JsonSerializer.Serialize(document.RootElement, Compact);
    }

    /// <summary>
    /// Options that write no whitespace, for a stream whose framing is the newline.
    /// </summary>
    private static readonly JsonSerializerOptions Compact = new(AgentToolJson.DefaultSerializerOptions) { WriteIndented = false };

    /// <summary>
    /// Turns a failure into a response.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>The response.</returns>
    /// <remarks>
    /// The library throws its own exceptions rather than picking a status code, because what a
    /// failed tool call should look like is the application's decision. This is that decision. The
    /// distinction worth preserving is refusal against fault: a refusal that reads as a fault
    /// invites a model to retry it, and an agent will accept the invitation.
    /// </remarks>
    public static IResult Describe(AgentToolException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            AgentToolNotFoundException => Results.NotFound(new { Error = exception.Message, Retry = false }),
            AgentToolAccessDeniedException => Results.Json(new { Error = exception.Message, Retry = false }, statusCode: StatusCodes.Status403Forbidden),
            AgentToolArgumentException => Results.BadRequest(new { Error = exception.Message, Retry = true, Note = "Correct the arguments and try again." }),
            _ => Results.Json(new { Error = exception.Message, Retry = false }, statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
