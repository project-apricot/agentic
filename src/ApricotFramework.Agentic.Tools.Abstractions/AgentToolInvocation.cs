using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// How an invocation travels with the arguments it belongs to.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AIFunctionArguments.Context"/> is where the AI abstractions already keep whatever a
/// caller wants a function to have, and <see cref="AIFunctionArguments.Services"/> is where they
/// keep the scope. Using both rather than adding a parameter is what keeps a tool an ordinary
/// <see cref="AIFunction"/>, callable by anything in the ecosystem.
/// </para>
/// <para>
/// The JSON payload rides along beside the named arguments on purpose. A model's arguments arrive
/// as a dictionary, which is what a foreign function reads; a surface of ours has JSON text, in
/// which an <c>Int64</c> past 2^53 survives. Carrying both means neither kind of tool pays for
/// the other's shape.
/// </para>
/// </remarks>
public static class AgentToolInvocation
{
    /// <summary>
    /// Where the JSON payload is kept.
    /// </summary>
    private static readonly object PayloadKey = typeof(AgentToolInvocation);

    /// <summary>
    /// Builds the arguments for a call, from JSON.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON, or null or empty where there are none.</param>
    /// <param name="context">Who is asking, and the scope this call runs in.</param>
    /// <returns>The arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="AgentToolArgumentException">Thrown when the arguments are not JSON or are not an object.</exception>
    public static AIFunctionArguments Create(string? argumentsJson, AgentToolContext context)
    {
        return Create(Parse(argumentsJson), context);
    }

    /// <summary>
    /// Builds the arguments for a call.
    /// </summary>
    /// <param name="payload">The arguments as a JSON object.</param>
    /// <param name="context">Who is asking, and the scope this call runs in.</param>
    /// <returns>The arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="AgentToolArgumentException">Thrown when <paramref name="payload"/> is not an object.</exception>
    public static AIFunctionArguments Create(JsonElement payload, AgentToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new AgentToolArgumentException($"Arguments have to be a JSON object, and these are {payload.ValueKind.ToString().ToLowerInvariant()}.");
        }

        var arguments = new AIFunctionArguments { Services = context.Services };

        // the named arguments too, so that a function which was not written against this library
        // - one read off a foreign server, one built from a delegate - reads what it expects
        foreach (var property in payload.EnumerateObject())
        {
            arguments[property.Name] = property.Value;
        }

        arguments.Context = new Dictionary<object, object?>
        {
            [typeof(AgentToolContext)] = context,
            [PayloadKey] = payload
        };

        return arguments;
    }

    /// <summary>
    /// Reads the arguments as JSON, where they were given that way.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The payload, or null where the caller built the arguments itself.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static JsonElement? GetPayload(this AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Context?.TryGetValue(PayloadKey, out var payload) == true && payload is JsonElement element
            ? element
            : null;
    }

    /// <summary>
    /// Reads the invocation, where there is one.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The invocation, or null where the caller supplied none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static AgentToolContext? GetAgentToolContext(this AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Context?.TryGetValue(typeof(AgentToolContext), out var context) == true
            ? context as AgentToolContext
            : null;
    }

    /// <summary>
    /// Reads the invocation, insisting on one.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The invocation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the caller supplied none.</exception>
    /// <remarks>
    /// Deliberately not an <see cref="AgentToolException"/>. Arriving without a context is a host
    /// that went around the executor, not a call a model got wrong - and a refusal a model is
    /// invited to retry is worse than a fault that stops.
    /// </remarks>
    public static AgentToolContext RequireAgentToolContext(this AIFunctionArguments arguments)
    {
        return arguments.GetAgentToolContext()
               ?? throw new InvalidOperationException(
                   "This tool was invoked without an invocation context. Run it through IAgentToolExecutor, " +
                   "or build the arguments with AgentToolInvocation.Create.");
    }

    /// <summary>
    /// Reads a JSON payload, treating nothing as an empty object.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON.</param>
    /// <returns>The payload.</returns>
    /// <exception cref="AgentToolArgumentException">Thrown when the arguments are not JSON.</exception>
    /// <remarks>
    /// An absent payload is read as an empty object rather than rejected. A model calling a tool
    /// that takes nothing may well send nothing, and refusing that would be a failure it cannot
    /// act on.
    /// </remarks>
    public static JsonElement Parse(string? argumentsJson)
    {
        try
        {
            // cloned, so the element outlives the document it was read from
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);

            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new AgentToolArgumentException("The arguments could not be read as JSON.", exception);
        }
    }
}
