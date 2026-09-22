using System.ComponentModel;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Adapters;
using ApricotFramework.Agentic.Tools.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;

namespace ApricotFramework.Agentic.Examples.SupportDesk.Foreign;

/// <summary>
/// Tools from a knowledge base the desk does not own.
/// </summary>
/// <remarks>
/// <para>
/// Stands in for a third party MCP server. A real one would be an <c>McpClient</c>, whose tools
/// are already <see cref="AIFunction"/> instances - so they arrive through
/// <see cref="AgentTool.Create"/> exactly as these do, and nothing else about the wiring differs.
/// </para>
/// <para>
/// Asynchronous because a real one could not be enumerated while the container was being built: a
/// connection has to be made first, and that is not something to do inside
/// <c>ConfigureServices</c>.
/// </para>
/// <para>
/// One of the three tools here is deliberately malformed - it describes itself with whitespace -
/// so the example shows what curation is for. See <c>AddSupportDeskAgentTools</c> for the wrapping.
/// </para>
/// <para>
/// Each is handed over as a descriptor carrying the authorization this application decided it
/// should have. A source that attached none would offer tools the authorization filter then
/// refuses, which is fail-closed and useless in equal measure.
/// </para>
/// </remarks>
public sealed class KnowledgeBaseToolSource : IAgentToolSource
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        // what the foreign server chose to call things, which is not our business to like
        IReadOnlyList<AgentToolDescriptor> tools =
        [
            Adapt(
                AIFunctionFactory.Create(
                    ([Description("What to search the knowledge base for.")] string query) =>
                        new { Articles = new[] { $"kb-1: how to {query}", $"kb-2: troubleshooting {query}" } },
                    "search",
                    "Searches the knowledge base for articles matching a query."),
                isDestructive: false),

            Adapt(
                AIFunctionFactory.Create(
                    ([Description("The identifier of the article to read.")] string id) => new { Id = id, Body = "…" },
                    "article",
                    "Reads one knowledge base article in full."),
                isDestructive: false),

            // no prose at all, so the declaration validator will refuse it
            Adapt(
                AIFunctionFactory.Create(([Description("The article to retire.")] string id) => new { Retired = id }, "retire", "   "),
                isDestructive: true)
        ];

        return ValueTask.FromResult(tools);
    }

    /// <summary>
    /// Wraps a foreign function on terms this application sets.
    /// </summary>
    /// <param name="function">The foreign function.</param>
    /// <param name="isDestructive">Whether calling it destroys anything.</param>
    /// <returns>The tool.</returns>
    /// <remarks>
    /// The options are where a host takes responsibility for a declaration it inherited. The
    /// behaviour flags are required, and the surface label is added here, because a foreign server
    /// has no way to know which surfaces exist here and may say nothing about whether its tools
    /// destroy anything.
    /// </remarks>
    private static AgentToolDescriptor Adapt(AIFunction function, bool isDestructive) =>
        new(AgentTool.Create(function, new AgentToolCreateOptions
        {
            // renamed into a space of our own, so a foreign "search" cannot shadow one of ours
            Name = $"support_kb_{function.Name}",
            IsReadOnly = !isDestructive,
            IsDestructive = isDestructive,
            IsOpenWorld = true,
            Labels = new Dictionary<string, object?>
            {
                [SupportDeskLabels.Sensitivity] = Sensitivity.Public,
                [SupportDeskLabels.Area] = "knowledge-base",
                [SupportDeskLabels.Surfaces] = SupportDeskSurfaces.Both,
                ["origin"] = "knowledge-base-co"
            }
        }),
        // nothing a foreign server hands over carries an authorization attribute, so whatever
        // should gate it is attached here - the same thing RequireAuthorization would have added
        [new AuthorizeAttribute(SupportDeskPolicies.TicketsRead)]);
}
