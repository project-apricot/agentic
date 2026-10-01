namespace ApricotFramework.Agentic.Tools.Grpc;

/// <summary>
/// Trailers a serving host attaches to a failed call, and their values.
/// </summary>
/// <remarks>
/// They tell a framework refusal from a tool failure, which share status codes such as
/// <c>NOT_FOUND</c>. A status without them was not described by the framework: read it by its code
/// only where the code is unambiguous, never an untrailed <c>NOT_FOUND</c>. Documented in <c>tools.proto</c>.
/// </remarks>
public static class AgentToolTrailers
{
    /// <summary>
    /// Whether the framework refused the call or the tool's operation failed.
    /// </summary>
    public const string Outcome = "apricot-agentic-outcome";

    /// <summary>
    /// Which refusal, or which kind of failure.
    /// </summary>
    public const string Reason = "apricot-agentic-reason";

    /// <summary>
    /// For a failure, whether retrying unchanged may succeed: <c>true</c> or <c>false</c>.
    /// </summary>
    public const string Retryable = "apricot-agentic-retryable";

    /// <summary>
    /// The framework refused the call before the tool ran.
    /// </summary>
    public const string Refused = "refused";

    /// <summary>
    /// The tool ran and its operation failed.
    /// </summary>
    public const string Failed = "failed";

    /// <summary>
    /// <see cref="Reason"/> values when the call was <see cref="Refused"/>.
    /// </summary>
    public static class Refusals
    {
        /// <summary>No tool is offered to this caller under the name.</summary>
        public const string NotFound = "not_found";

        /// <summary>The caller may not use the tool.</summary>
        public const string AccessDenied = "access_denied";

        /// <summary>The tool needs a caller and nobody is calling.</summary>
        public const string Unauthenticated = "unauthenticated";

        /// <summary>The arguments could not be read.</summary>
        public const string InvalidArguments = "invalid_arguments";

        /// <summary>The tool is declared but the serving host cannot run it.</summary>
        public const string NotInvocable = "not_invocable";
    }

    /// <summary>
    /// <see cref="Reason"/> values when the call <see cref="Failed"/>.
    /// </summary>
    public static class Failures
    {
        /// <summary>A failure the caller can do nothing about.</summary>
        public const string Fault = "fault";

        /// <summary>The requested subject does not exist.</summary>
        public const string NotFound = "not_found";

        /// <summary>The request was rejected as invalid.</summary>
        public const string Invalid = "invalid";

        /// <summary>The request conflicts with the current state.</summary>
        public const string Conflict = "conflict";

        /// <summary>The operation refused the caller.</summary>
        public const string Denied = "denied";

        /// <summary>The operation needed a caller and had none.</summary>
        public const string Unauthenticated = "unauthenticated";

        /// <summary>The caller has asked too often.</summary>
        public const string RateLimited = "rate_limited";

        /// <summary>Something the tool depends on could not be reached.</summary>
        public const string Unavailable = "unavailable";

        /// <summary>The operation did not finish in time.</summary>
        public const string Timeout = "timeout";
    }
}
