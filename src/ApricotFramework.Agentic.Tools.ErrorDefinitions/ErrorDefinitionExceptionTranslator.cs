using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.ErrorDefinitions;

namespace ApricotFramework.Agentic.Tools.ErrorDefinitions;

/// <summary>
/// Describes an <see cref="ErrorDefinitionException"/> as an <see cref="AgentToolFailedException"/>.
/// </summary>
/// <remarks>
/// The first error's kind decides the failure kind. Messages carry their codes, and a validation
/// failure lists every error, so a model can correct all of them at once.
/// </remarks>
public sealed class ErrorDefinitionExceptionTranslator : IAgentToolExceptionTranslator
{
    /// <inheritdoc />
    public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool)
    {
        if (exception is not ErrorDefinitionException { Errors.Count: > 0 } failure)
        {
            return null;
        }

        var kind = Kind(failure.Errors[0].Kind);

        var described = kind == AgentToolFailureKind.Invalid ? failure.Errors : [failure.Errors[0]];

        return new AgentToolFailedException(kind, string.Join(" ", described.Select(Describe)), exception);
    }

    /// <summary>
    /// Maps an error kind to a failure kind.
    /// </summary>
    /// <param name="kind">The error kind.</param>
    /// <returns>The failure kind; a fault for any kind without a closer match.</returns>
    public static AgentToolFailureKind Kind(string? kind) => kind switch
    {
        ErrorKinds.NotFound => AgentToolFailureKind.NotFound,
        ErrorKinds.Validation or ErrorKinds.OutOfRange => AgentToolFailureKind.Invalid,
        ErrorKinds.AlreadyExists or ErrorKinds.PreconditionFailed or ErrorKinds.Aborted => AgentToolFailureKind.Conflict,
        ErrorKinds.AccessDenied => AgentToolFailureKind.Denied,
        ErrorKinds.NotAuthenticated => AgentToolFailureKind.Unauthenticated,
        ErrorKinds.ResourceExhausted => AgentToolFailureKind.RateLimited,
        ErrorKinds.Unavailable => AgentToolFailureKind.Unavailable,
        ErrorKinds.Timeout => AgentToolFailureKind.Timeout,
        _ => AgentToolFailureKind.Fault
    };

    /// <summary>
    /// Describes one error, with its code.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The description.</returns>
    private static string Describe(ErrorDefinition error) =>
        string.IsNullOrWhiteSpace(error.Code) ? error.Message : $"{error.Message} ({error.Code})";
}
