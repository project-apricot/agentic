using ApricotFramework.Agentic.Examples.SupportDesk.Auditing;
using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Filters;
using ApricotFramework.Agentic.Examples.SupportDesk.Foreign;
using ApricotFramework.Agentic.Examples.SupportDesk.Tools;
using ApricotFramework.Agentic.Examples.SupportDesk.Validators;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.Extensions;
using ApricotFramework.Agentic.Tools.Sources;
using ApricotFramework.Agentic.Tools.Validators;
using ApricotFramework.Agentic.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApricotFramework.Agentic.Examples.SupportDesk;

/// <summary>
/// Wires up the support desk's tools.
/// </summary>
/// <remarks>
/// One place, so a reader can see every way a tool can reach the registry without hunting for it.
/// </remarks>
public static class SupportDeskAgentTools
{
    /// <summary>
    /// Adds the support desk's tools and everything that runs them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>
    /// The builder, so the host can go on to say what only it knows - who is asking, most of all.
    /// </returns>
    /// <remarks>
    /// Returns the builder rather than the collection on purpose. This is a library of tools, not
    /// a host: it composes the machinery and leaves the host-shaped decisions to whoever is
    /// hosting, which is what the chain is for.
    /// </remarks>
    public static IAgentToolsBuilder AddSupportDeskAgentTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SupportDeskStore>();

        // the machinery, and the decisions where sequence is part of the meaning. authorization
        // is asked for rather than assumed: a host with no policies configured should not be
        // handed a filter that cannot resolve IAuthorizationService, and forgetting it is caught
        // rather than permitted - the tripwire refuses to compose a registry whose tools declare
        // authorization nothing enforces
        var tools = services.AddAgentToolsCore()
            .WithAuthorization()

            // what belongs around every call rather than inside every tool
            .DecorateInvoker<AuditingAgentToolInvoker>();

        // which surfaces exist is this application's idea, so it brings its own filter for it
        services.AddAgentToolFilter<SurfaceAgentToolFilter>();

        // and its own rules, checked as the registry composes. the library registers none of
        // these, because what a tool ought to carry beyond a name is a judgment
        services.AddAgentToolValidator<DescriptionDeclaredValidator>();
        services.AddAgentToolValidator<ConsistentBehaviourValidator>();
        services.AddAgentToolValidator<SupportDeskNamingValidator>();
        services.AddAgentToolValidator<SensitivityDeclaredValidator>();

        // two classes of methods, and one tool written by hand. the list is short enough to read,
        // which is the shape to prefer while it stays that way
        services.AddAgentToolType<TicketTools>();
        services.AddAgentToolType<CustomerTools>();
        services.AddAgentToolType<OutsideTools>();

        // the one that has to be a class: its result arrives as a sequence, and only an AgentTool
        // can produce items rather than one value
        services.AddAgentTool<TicketsListTool>();

        // a tool with no class and no method of its own: a delegate, gated at registration because
        // there is nothing to hang an attribute on
        services.AddAgentTool(
            Summarise(),
            new AgentToolDeclaration
            {
                Name = "support_text_summarise",
                Title = "Summarise text",
                Description = "Summarises a passage of text to roughly one line.",
                IsReadOnly = true,
                IsDestructive = false,
                Labels = new Dictionary<string, object?>
                {
                    [SupportDeskLabels.Sensitivity] = Sensitivity.Public,
                    [SupportDeskLabels.Surfaces] = SupportDeskSurfaces.Both
                }
            },
            tool => tool.RequireAuthorization(SupportDeskPolicies.TicketsRead));

        // tools from somewhere we do not control, looked over before being offered. prefixing is
        // already done as they are adapted; this drops whatever still would not pass, so one bad
        // declaration from a third party costs us that tool rather than every tool
        services.AddSingleton<KnowledgeBaseToolSource>();


        services.AddAgentToolSource(provider => new CuratingAgentToolSource(
            provider.GetRequiredService<KnowledgeBaseToolSource>(),
            AgentToolCuration.DropRejected(
                provider.GetServices<IAgentToolValidator>(),
                (tool, why) => SupportDeskLog.ForeignToolRejected(
                    provider.GetRequiredService<ILogger<KnowledgeBaseToolSource>>(), why, tool.Name))));

        return tools;
    }

    /// <summary>
    /// A function standing in for anything a host has that is not a tool class.
    /// </summary>
    /// <returns>The function.</returns>
    private static AIFunction Summarise() => AIFunctionFactory.Create(
        ([System.ComponentModel.Description("The text to summarise.")] string text) =>
            new { Summary = text.Length <= 60 ? text : text[..57] + "..." },
        "summarise",
        "Summarises a passage of text to roughly one line.");
}
