using System.ComponentModel;
using ApricotFramework.ErrorDefinitions;

namespace ApricotFramework.Agentic.Tools.ErrorDefinitions.Tests;

/// <summary>Tools that fail the way a host using error definitions fails.</summary>
[AgentToolType]
public sealed class LedgerTools
{
    [AgentTool("ledger_get", ReadOnly = true)]
    [Description("Reads an entry that is not there.")]
    public static string Get([Description("The entry.")] long id) =>
        throw new ErrorDefinitionException([new ErrorDefinition { Kind = ErrorKinds.NotFound, Code = "LEDGER_ENTRY_NOT_FOUND", Message = $"No entry {id}." }]);

    [AgentTool("ledger_post", ReadOnly = false)]
    [Description("Posts an entry that fails validation twice.")]
    public static string Post() =>
        throw new ErrorDefinitionException(
        [
            new ErrorDefinition { Kind = ErrorKinds.Validation, Code = "AMOUNT_REQUIRED", Message = "An amount is required." },
            new ErrorDefinition { Kind = ErrorKinds.Validation, Code = "ACCOUNT_REQUIRED", Message = "An account is required." }
        ]);

    [AgentTool("ledger_sync", ReadOnly = false)]
    [Description("Syncs with a ledger that is down.")]
    public static string Sync() =>
        throw new ErrorDefinitionException([new ErrorDefinition { Kind = ErrorKinds.Unavailable, Message = "The ledger is down." }]);

    [AgentTool("ledger_crash", ReadOnly = true)]
    [Description("Fails with something that is not an error definition.")]
    public static string Crash() => throw new InvalidOperationException("boom");
}
