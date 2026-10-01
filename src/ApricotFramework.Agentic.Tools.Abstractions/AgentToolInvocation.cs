using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools;

/// <summary>
/// Carries the invocation context and JSON payload inside <see cref="AIFunctionArguments"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="AIFunctionArguments.Context"/> and <see cref="AIFunctionArguments.Services"/> so
/// tools stay ordinary <see cref="AIFunction"/>s. The JSON payload travels beside the named
/// arguments so an <c>Int64</c> beyond 2^53 survives.
/// </remarks>
public static class AgentToolInvocation
{
    /// <summary>
    /// Key under which the JSON payload is stored.
    /// </summary>
    private static readonly object PayloadKey = typeof(AgentToolInvocation);

    /// <summary>
    /// Builds call arguments from JSON.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON, or null or empty for none.</param>
    /// <param name="context">The invocation context.</param>
    /// <returns>The arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="AgentToolArgumentException">Thrown when the arguments are not a JSON object.</exception>
    public static AIFunctionArguments Create(string? argumentsJson, AgentToolContext context)
    {
        return Create(Parse(argumentsJson), context);
    }

    /// <summary>
    /// Builds call arguments from a JSON object.
    /// </summary>
    /// <param name="payload">The arguments as a JSON object.</param>
    /// <param name="context">The invocation context.</param>
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
    /// Gets the JSON payload, if the arguments were built from one.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The payload, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static JsonElement? GetPayload(this AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Context?.TryGetValue(PayloadKey, out var payload) == true && payload is JsonElement element
            ? element
            : null;
    }

    /// <summary>
    /// Gets the invocation context, if any.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The context, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static AgentToolContext? GetAgentToolContext(this AIFunctionArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Context?.TryGetValue(typeof(AgentToolContext), out var context) == true
            ? context as AgentToolContext
            : null;
    }

    /// <summary>
    /// Gets the invocation context, throwing if absent.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no context.</exception>
    /// <remarks>
    /// Not an <see cref="AgentToolException"/>: a missing context means the host bypassed the executor,
    /// which should fault rather than invite a model to retry.
    /// </remarks>
    public static AgentToolContext RequireAgentToolContext(this AIFunctionArguments arguments)
    {
        return arguments.GetAgentToolContext()
               ?? throw new InvalidOperationException(
                   "This tool was invoked without an invocation context. Run it through IAgentToolExecutor, " +
                   "or build the arguments with AgentToolInvocation.Create.");
    }

    /// <summary>
    /// Parses a JSON payload, treating empty input as an empty object.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON.</param>
    /// <returns>The payload.</returns>
    /// <exception cref="AgentToolArgumentException">Thrown when the arguments are not valid JSON.</exception>
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
