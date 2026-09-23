using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Model;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Tools;

/// <summary>
/// Every ticket, reported as they come.
/// </summary>
/// <remarks>
/// <para>
/// The one tool here written by hand against <see cref="AgentTool"/> rather than as a method, and
/// the reason is the only one that justifies it: a result that arrives as a sequence. An
/// <see cref="AIFunction"/> returns one value, so <see cref="AgentTool.InvokeStreamingAsync"/> is
/// the only way to hand a caller items before the last one exists.
/// </para>
/// <para>
/// What that costs is visible below - the schemas, the flags and the argument handling are all
/// stated by hand, where a method gets them from the function factory. Worth it here, and not
/// worth it for the other fourteen tools on this desk.
/// </para>
/// <para>
/// A caller that cannot stream still sees the whole array: <see cref="InvokeCoreAsync"/> collects,
/// which is what an MCP <c>tools/call</c> and a chat client get.
/// </para>
/// </remarks>
/// <param name="store">Where the tickets are.</param>
[Authorize(Policy = SupportDeskPolicies.TicketsRead)]
[AgentToolLabel(SupportDeskLabels.Area, "support-desk")]
[AgentToolLabel(SupportDeskLabels.Surfaces, SupportDeskSurfaces.Both)]
[Sensitivity(Sensitivity.Internal)]
public sealed class TicketsListTool(SupportDeskStore store) : AgentTool
{
    /// <summary>A tool that takes nothing.</summary>
    private static readonly JsonElement NoArguments = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone();

    /// <inheritdoc />
    public override string Name => "support_tickets_list";

    /// <inheritdoc />
    public override string Title => "List tickets";

    /// <inheritdoc />
    public override string Description =>
        "Lists every support ticket on the desk, newest first, with its subject, status and assignee. " +
        "Use this to see what is outstanding, or to find a ticket's identifier before reading it in full. " +
        "Use support_tickets_search instead when looking for something specific.";

    /// <inheritdoc />
    public override bool IsReadOnly => true;

    /// <inheritdoc />
    public override bool IsDestructive => false;

    /// <inheritdoc />
    public override AgentToolResultKind ResultKind => AgentToolResultKind.Sequence;

    /// <inheritdoc />
    public override JsonElement JsonSchema => NoArguments;

    /// <inheritdoc />
    /// <remarks>The assembled array, not one item, so no consumer has to reconstruct the outer shape.</remarks>
    public override JsonElement? ReturnJsonSchema => AgentToolJson.Schema<IReadOnlyList<TicketSummary>>();

    /// <inheritdoc />
    public override async IAsyncEnumerable<object?> InvokeStreamingAsync(
        AIFunctionArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var ticket in store.Tickets())
        {
            await Task.Yield();

            yield return TicketTools.Summarise(ticket);
        }
    }

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        ValueTask.FromResult<object?>(store.Tickets().Select(TicketTools.Summarise).ToList());
}
