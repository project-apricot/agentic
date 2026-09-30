using System.Text.Json;
using ApricotFramework.Agentic.Examples.SupportDesk;
using ApricotFramework.Agentic.Examples.Web;
using ApricotFramework.Agentic.Examples.Web.Auth;
using ApricotFramework.Agentic.Tools;
using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Extensions;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(DemoAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(DemoAuthenticationHandler.SchemeName, null);

// the policies the tools name in their attributes. the library never learns what they mean
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SupportDeskPolicies.TicketsRead, policy => policy.RequireClaim("scope", SupportDeskPolicies.TicketsRead))
    .AddPolicy(SupportDeskPolicies.TicketsWrite, policy => policy.RequireClaim("scope", SupportDeskPolicies.TicketsWrite))
    .AddPolicy(SupportDeskPolicies.TicketsAdmin, policy => policy.RequireClaim("scope", SupportDeskPolicies.TicketsAdmin))
    .AddPolicy(SupportDeskPolicies.CustomersRead, policy => policy.RequireClaim("scope", SupportDeskPolicies.CustomersRead));

builder.Services.AddHttpContextAccessor();

// the tool library composes the machinery and hands back the builder; the host says who is
// asking, because only the host knows. the surface is this application's own idea, so the
// library carries nothing for it and this is where it is read
builder.Services.AddSupportDeskAgentTools()
    .WithContext<SupportDeskAgentToolContextFactory>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Nothing composes the registry here any more: a malformed declaration of ours already stopped
// the host, in the hosted service AddAgentTools registers. The registry itself composes per call,
// because a source reaching an upstream server can answer differently for a different caller.

// What this surface advertises. Filtered by the same authorizer that would gate the call, so
// nothing listed here then refuses the caller - try it with no headers, and it comes back empty.
app.MapGet("/tools", async (IAgentToolExecutor executor, CancellationToken cancellationToken) =>
{
    var tools = await executor.GetAvailableToolsAsync(cancellationToken);

    return Results.Ok(tools.Select(tool => ToolCalls.Describe(tool)));
});

// One declaration in full, schemas included, for whoever is writing the arguments.
app.MapGet("/tools/{name}", async (IAgentToolExecutor executor, string name, CancellationToken cancellationToken) =>
{
    var tools = await executor.GetAvailableToolsAsync(cancellationToken);

    var tool = tools.FirstOrDefault(entry => entry.Name == name);

    return tool is null
        ? Results.NotFound(new { Error = $"No tool '{name}' is offered to this caller." })
        : Results.Ok(ToolCalls.Describe(tool, schemas: true));
});

// Invoke and report the complete result. What an MCP tools/call maps onto, since the protocol
// returns one result per call.
app.MapPost("/tools/{name}", async (IAgentToolExecutor executor, HttpContext http, string name, CancellationToken cancellationToken) =>
{
    var argumentsJson = await ToolCalls.ReadArgumentsAsync(http, cancellationToken);

    try
    {
        var result = await executor.InvokeCompleteAsync(name, argumentsJson, cancellationToken);

        return Results.Text(result, "application/json");
    }
    catch (AgentToolException exception)
    {
        return ToolCalls.Describe(exception);
    }
});

// Invoke and report each item as it arrives, one JSON object per line. What an internal agent
// loop would read, and what a sequence tool is for.
app.MapPost("/tools/{name}/stream", async (IAgentToolExecutor executor, HttpContext http, string name, CancellationToken cancellationToken) =>
{
    var argumentsJson = await ToolCalls.ReadArgumentsAsync(http, cancellationToken);

    http.Response.ContentType = "application/x-ndjson";

    try
    {
        await foreach (var chunk in executor.InvokeAsync(name, argumentsJson, cancellationToken))
        {
            // one object per line, so the newline is the framing
            await http.Response.WriteAsync(ToolCalls.OneLine(chunk) + '\n', cancellationToken);
            await http.Response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (AgentToolException exception)
    {
        // a failure part way through a sequence fails the whole call, and whatever arrived is not
        // a result. the line says so rather than leaving a truncated array looking complete
        await http.Response.WriteAsync(
            ToolCalls.OneLine(JsonSerializer.Serialize(new { Error = exception.Message, Partial = true })) + '\n',
            cancellationToken);
    }
});

await app.RunAsync();
