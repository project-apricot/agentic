using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApricotFramework.Agentic.Tools.Mcp.Server;

/// <summary>
/// Handles MCP <c>tools/list</c> and <c>tools/call</c>.
/// </summary>
/// <remarks>
/// Handlers rather than a fixed tool collection, so a listing can differ per caller and change at
/// runtime. Each call goes through <see cref="IAgentToolExecutor"/> and its filters.
/// </remarks>
/// <param name="executor">The tool executor.</param>
public sealed class AgentToolMcpHandlers(IAgentToolExecutor executor)
{
    /// <summary>
    /// Handles <c>tools/list</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools offered to this caller.</returns>
    public async ValueTask<ListToolsResult> ListAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var offered = await this.OfferedAsync(cancellationToken).ConfigureAwait(false);

        var natural = SupportsNaturalOutputSchemas(request.Server.NegotiatedProtocolVersion);

        return new ListToolsResult { Tools = [.. offered.Select(tool => natural ? tool.ProtocolTool : ForLegacyWire(tool.ProtocolTool))] };
    }

    /// <summary>
    /// The first protocol revision allowing a non-object output schema (SEP-2106).
    /// </summary>
    private const string NaturalOutputSchemasVersion = "2026-07-28";

    /// <summary>
    /// Checks whether a protocol version accepts output schemas in their natural shape.
    /// </summary>
    /// <param name="protocolVersion">The negotiated version, or null before negotiation.</param>
    /// <returns>True from <c>2026-07-28</c> on.</returns>
    /// <remarks>
    /// Mirrors the SDK's own check, which it applies in its built-in <c>tools/list</c> handler and
    /// when wrapping structured content; a custom handler has to apply it to the listing itself.
    /// </remarks>
    private static bool SupportsNaturalOutputSchemas(string? protocolVersion) =>
        protocolVersion is not null && string.CompareOrdinal(protocolVersion, NaturalOutputSchemasVersion) >= 0;

    /// <summary>
    /// Gives a tool the output schema an older client expects.
    /// </summary>
    /// <param name="tool">The tool in its natural shape.</param>
    /// <returns>The tool with a non-object schema wrapped under <c>result</c>, or itself.</returns>
    /// <remarks>
    /// The SDK's own transform, which is internal: a non-object schema is wrapped, exactly
    /// <c>["object","null"]</c> is narrowed to <c>object</c>. The SDK wraps the structured content
    /// to match on the same condition, so the two agree.
    /// </remarks>
    private static Tool ForLegacyWire(Tool tool)
    {
        if (tool.OutputSchema is not { } schema || IsObject(schema))
        {
            return tool;
        }

        var node = JsonNode.Parse(schema.GetRawText());

        JsonNode legacy = IsNullableObject(node)
            ? Narrowed(node!.AsObject())
            : new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject { ["result"] = node },
                ["required"] = new JsonArray("result")
            };

        return new Tool
        {
            Name = tool.Name,
            Title = tool.Title,
            Description = tool.Description,
            InputSchema = tool.InputSchema,
            OutputSchema = JsonSerializer.SerializeToElement(legacy),
            Annotations = tool.Annotations,
            Icons = tool.Icons,
            Meta = tool.Meta
        };
    }

    /// <summary>
    /// Checks for a schema whose type is plainly <c>object</c>.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>True where it needs no change.</returns>
    private static bool IsObject(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object
        && schema.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.ValueEquals("object");

    /// <summary>
    /// Checks for a schema typed exactly <c>["object","null"]</c>.
    /// </summary>
    /// <param name="node">The schema.</param>
    /// <returns>True where only the type needs narrowing.</returns>
    private static bool IsNullableObject(JsonNode? node) =>
        node is JsonObject schema
        && schema.TryGetPropertyValue("type", out var type)
        && type is JsonArray { Count: 2 } types
        && types.Any(entry => (string?)entry == "object")
        && types.Any(entry => (string?)entry == "null");

    /// <summary>
    /// Narrows a nullable object schema to <c>object</c>.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>The same schema, narrowed.</returns>
    private static JsonObject Narrowed(JsonObject schema)
    {
        schema["type"] = "object";

        return schema;
    }

    /// <summary>
    /// Handles <c>tools/call</c>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result; an error result if the tool was refused or failed.</returns>
    /// <exception cref="McpException">Thrown when no tool is offered to this caller under the name.</exception>
    /// <remarks>
    /// Looks up only the called tool. A missing tool is a protocol error; every other refusal or
    /// failure is an <c>isError</c> result with guidance on whether to retry.
    /// </remarks>
    public async ValueTask<CallToolResult> CallAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Params.Name;

        var function = await executor.GetAvailableFunctionAsync(name, cancellationToken).ConfigureAwait(false)
                       ?? throw new McpException($"No tool is offered as '{name}'.");

        try
        {
            return await Describe(function).InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AgentToolNotFoundException exception)
        {
            // gone between the lookup and the call; to the caller that is a tool that is not there
            throw new McpException($"{exception.Message} Do not retry; list the tools again.", exception);
        }
        catch (AgentToolException exception)
        {
            return Error(Describe(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not McpException)
        {
            // nobody described it, so its message is not known to be fit for a model; say only that
            // it failed, as the SDK does for its own tools
            return Error($"The tool '{name}' failed. Do not retry unchanged.");
        }
    }

    /// <summary>
    /// Builds an error result.
    /// </summary>
    /// <param name="text">What the model reads.</param>
    /// <returns>The result.</returns>
    private static CallToolResult Error(string text) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = text }]
    };

    /// <summary>
    /// Builds the protocol tools offered to this caller.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tools.</returns>
    private async ValueTask<IReadOnlyList<McpServerTool>> OfferedAsync(CancellationToken cancellationToken)
    {
        var functions = await executor.GetAvailableFunctionsAsync(cancellationToken).ConfigureAwait(false);

        return [.. functions.Select(Describe)];
    }

    /// <summary>
    /// Converts a tool to its protocol form.
    /// </summary>
    /// <param name="function">The tool.</param>
    /// <returns>The protocol tool.</returns>
    /// <remarks>
    /// Behavior hints are always stated explicitly, since clients decide confirmation on them.
    /// </remarks>
    private static McpServerTool Describe(AIFunction function)
    {
        var declaration = function as IAgentToolDeclaration;

        return McpServerTool.Create(function, new McpServerToolCreateOptions
        {
            Title = declaration?.Title,
            ReadOnly = declaration?.IsReadOnly,
            Destructive = declaration?.IsDestructive,
            Idempotent = declaration?.IsIdempotent,
            OpenWorld = declaration?.IsOpenWorld,
            UseStructuredContent = function.ReturnJsonSchema is not null
        });
    }

    /// <summary>
    /// Builds the error result text, including what to do next.
    /// </summary>
    /// <param name="exception">The refusal or failure.</param>
    /// <returns>The error text.</returns>
    /// <remarks>
    /// Refusals must not read as retryable.
    /// </remarks>
    private static string Describe(AgentToolException exception) => exception switch
    {
        AgentToolUnauthenticatedException => $"{exception.Message} The user is not signed in, or their session has ended; they need to sign in again. Do not retry.",
        AgentToolAccessDeniedException => $"{exception.Message} The user does not hold the access this needs. Do not retry; ask the user to request it.",
        AgentToolArgumentException => $"{exception.Message} Correct the arguments and try again.",
        AgentToolNotInvocableException => $"{exception.Message} Do not retry.",
        AgentToolFailedException failed => $"{failed.Message} {Advice(failed)}",
        _ => $"{exception.Message} Do not retry unchanged."
    };

    /// <summary>
    /// Builds the advice for an operation failure.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns>The advice.</returns>
    private static string Advice(AgentToolFailedException failure) => failure.Kind switch
    {
        AgentToolFailureKind.NotFound => "Nothing matches what was asked for; check the identifiers rather than retrying unchanged.",
        AgentToolFailureKind.Invalid => "The request was rejected as invalid; correct it and try again.",
        AgentToolFailureKind.Conflict => "The current state does not allow this; read it again before deciding whether to retry.",
        AgentToolFailureKind.Denied => "The user is not permitted to do this. Do not retry; ask the user to request access.",
        AgentToolFailureKind.Unauthenticated => "The user needs to sign in again. Do not retry.",
        AgentToolFailureKind.RateLimited => "Too many requests; wait before trying again.",
        _ when failure.IsRetryable => "This may succeed if retried in a moment.",
        _ => "Do not retry unchanged."
    };
}
