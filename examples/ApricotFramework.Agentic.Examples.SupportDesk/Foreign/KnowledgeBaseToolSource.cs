using System.ComponentModel;
using ApricotFramework.Agentic.Tools;
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
    public ValueTask<IReadOnlyList<AgentToolDescriptor>> GetToolsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    /// <remarks>
    /// Everything this source offers sits under one prefix, so a name outside it is answered
    /// without reaching the foreign server at all - the cheap part of a lookup, and the part only
    /// the source can know.
    /// </remarks>
    public async ValueTask<AgentToolDescriptor?> FindAsync(string name, IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        if (!name.StartsWith("support_kb_", StringComparison.Ordinal))
        {
            return null;
        }

        var tools = await this.GetToolsAsync(context, cancellationToken).ConfigureAwait(false);

        return tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));
    }

    /// <summary>
    /// Wraps a foreign function on terms this application sets.
    /// </summary>
    /// <param name="function">The foreign function.</param>
    /// <param name="isDestructive">Whether calling it destroys anything.</param>
    /// <returns>The tool.</returns>
    /// <remarks>
    /// <para>
    /// The declaration is where a host takes responsibility for something it inherited. The
    /// behaviour flags are required, and the surface label is added here, because a foreign
    /// server has no way to know which surfaces exist here and may say nothing about whether its
    /// tools destroy anything.
    /// </para>
    /// <para>
    /// Nothing wraps the function. It goes into the descriptor as it arrived, so a consumer
    /// reaching through it for what it really is still finds it - which matters most for a tool
    /// read off a live MCP client, where the thing behind it is an <c>McpClientTool</c>.
    /// </para>
    /// </remarks>
    private static AgentToolDescriptor Adapt(AIFunction function, bool isDestructive) =>
        new(
            function,
            new AgentToolDeclaration
            {
                // offered in a space of our own, so a foreign "search" cannot shadow one of ours
                Name = $"support_kb_{function.Name}",
                Description = function.Description,
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
            },
            // nothing a foreign server hands over carries an authorization attribute, so whatever
            // should gate it is attached here - the same thing RequireAuthorization would add
            [new AuthorizeAttribute(SupportDeskPolicies.TicketsRead)]);
}
