using ApricotFramework.Agentic.Examples.SupportDesk.Auditing;
using ApricotFramework.Agentic.Examples.SupportDesk.Data;
using ApricotFramework.Agentic.Examples.SupportDesk.Filters;
using ApricotFramework.Agentic.Examples.SupportDesk.Foreign;
using ApricotFramework.Agentic.Examples.SupportDesk.Tools;
using ApricotFramework.Agentic.Examples.SupportDesk.Validators;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.Options;
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
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSupportDeskAgentTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SupportDeskStore>();

        // the registry, the invoker, and the tripwire. there is nothing to configure
        services.AddAgentTools();

        // authorization, asked for separately: a host with no policies configured should not be
        // handed a filter that cannot resolve IAuthorizationService. forgetting this is caught
        // rather than permitted - the tripwire refuses to compose a registry whose tools declare
        // authorization nothing enforces
        services.AddAgentToolAuthorization();

        // which surfaces exist is this application's idea, so it brings its own filter for it
        services.AddAgentToolFilter<SurfaceAgentToolFilter>();

        // and its own rules, checked as the registry composes. the library registers none of
        // these, because what a tool ought to carry beyond a name is a judgment
        services.AddAgentToolValidator<DescriptionDeclaredValidator>();
        services.AddAgentToolValidator<ConsistentBehaviourValidator>();
        services.AddAgentToolValidator<SupportDeskNamingValidator>();
        services.AddAgentToolValidator<SensitivityDeclaredValidator>();

        // named one at a time, which is the shape to prefer while a surface is small enough to
        // read. the list is then the surface, and somebody has to decide to add to it
        services.AddAgentTool<TicketsListTool>();
        services.AddAgentTool<TicketsGetTool>();
        services.AddAgentTool<TicketsSearchTool>();
        services.AddAgentTool<TicketsCreateTool>();
        services.AddAgentTool<TicketsReassignTool>();
        services.AddAgentTool<TicketsCloseTool>();
        services.AddAgentTool<NotesAddTool>();
        services.AddAgentTool<TicketsDeleteTool>();
        services.AddAgentTool<TicketsPurgeTool>();
        services.AddAgentTool<CustomersListTool>();
        services.AddAgentTool<CustomersGetTool>();
        services.AddAgentTool<CompanyRegistryLookupTool>();
        services.AddAgentTool<ServiceStatusTool>();

        // a tool with no class of its own: a delegate wrapped as a function, gated at registration
        // because there is no type to hang an attribute on
        services.AddAgentTool(Summarise(), new AgentToolCreateOptions
        {
            Name = "support_text_summarise",
            Title = "Summarise text",
            IsReadOnly = true,
            IsDestructive = false,
            Labels = new Dictionary<string, object?>
            {
                [SupportDeskLabels.Sensitivity] = Sensitivity.Public,
                [SupportDeskLabels.Surfaces] = SupportDeskSurfaces.Both
            }
        }).RequireAuthorization(SupportDeskPolicies.TicketsRead);

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

        // what belongs around every call rather than inside every tool
        services.DecorateAgentToolInvoker<AuditingAgentToolInvoker>();

        return services;
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
