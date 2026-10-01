using ApricotFramework.Agentic.Tools.Exceptions;
using ApricotFramework.Agentic.Tools.Filters;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Mcp.Server.Tests;

/// <summary>
/// <c>tools/call</c> over the executor: a missing tool is a protocol error, and anything that
/// happens once there is a tool comes back as a result the model can read and act on.
/// </summary>
public sealed class AgentToolMcpHandlersTests : IAsyncDisposable
{
    private readonly McpServer server = McpServer.Create(new StreamServerTransport(new MemoryStream(), new MemoryStream()), new McpServerOptions());

    private readonly Counting authorization = new();

    private ServiceProvider Host()
    {
        var services = new ServiceCollection();

        services.AddSingleton(this.authorization);
        services.AddAgentToolsCore();
        services.AddAgentToolType<InvoiceTools>();
        services.AddAgentToolAuthorizationFilter<CountingFilter>();
        services.AddAgentToolExceptionTranslator<Translating>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private Task<CallToolResult> Call(ServiceProvider host, string name, object? arguments = null)
    {
        var parameters = new CallToolRequestParams
        {
            Name = name,
            Arguments = arguments is null
                ? null
                : JsonSerializer.SerializeToElement(arguments).EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone())
        };

        var request = new RequestContext<CallToolRequestParams>(this.server, new JsonRpcRequest { Method = RequestMethods.ToolsCall }, parameters);

        return new AgentToolMcpHandlers(host.GetRequiredService<IAgentToolExecutor>()).CallAsync(request, TestContext.Current.CancellationToken).AsTask();
    }

    [Fact]
    public async Task ACall_ReturnsTheToolsResult()
    {
        await using var host = Host();

        var result = await this.Call(host, "invoices_get", new { id = 7 });

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("invoice:7", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryPublishedOutputSchema_HasAnObjectRoot_AsTheProtocolRequires()
    {
        await using var host = Host();

        var request = new RequestContext<ListToolsRequestParams>(this.server, new JsonRpcRequest { Method = RequestMethods.ToolsList }, new ListToolsRequestParams());

        var listed = await new AgentToolMcpHandlers(host.GetRequiredService<IAgentToolExecutor>()).ListAsync(request, TestContext.Current.CancellationToken);

        Assert.All(
            listed.Tools.Where(tool => tool.OutputSchema is not null),
            tool => Assert.Equal("object", tool.OutputSchema!.Value.GetProperty("type").GetString()));

        // a list is published the way the SDK returns it: wrapped under "result"
        var list = listed.Tools.Single(tool => tool.Name == "invoices_list").OutputSchema!.Value;

        Assert.Equal("array", list.GetProperty("properties").GetProperty("result").GetProperty("type").GetString());
    }

    [Fact]
    public async Task AListResult_IsReturnedUnderResult_MatchingThePublishedSchema()
    {
        await using var host = Host();

        var result = await this.Call(host, "invoices_list");

        Assert.Equal([1L, 2L], result.StructuredContent!.Value.GetProperty("result").Deserialize<long[]>()!);
    }

    [Fact]
    public async Task ACall_PutsOnlyThatToolToAuthorization_NotEveryToolOnTheSurface()
    {
        await using var host = Host();

        await this.Call(host, "invoices_get", new { id = 7 });

        // once to find it, once when the call runs - and never the other tools
        Assert.Equal(["invoices_get", "invoices_get"], this.authorization.Asked);
    }

    [Fact]
    public async Task AToolThatIsNotOffered_IsAProtocolError()
    {
        await using var host = Host();

        await Assert.ThrowsAsync<McpException>(() => this.Call(host, "invoices_unknown"));
    }

    [Fact]
    public async Task AToolTheCallerMayNotUse_IsNotOffered_AndSoAProtocolError()
    {
        await using var host = Host();

        this.authorization.Refuse("invoices_void");

        await Assert.ThrowsAsync<McpException>(() => this.Call(host, "invoices_void", new { id = 1 }));
    }

    [Fact]
    public async Task AFailureOfTheOperation_IsAnErrorResult_SayingWhatToDo()
    {
        await using var host = Host();

        var result = await this.Call(host, "invoices_missing", new { id = 9 });

        Assert.True(result.IsError);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;

        Assert.Contains("No invoice 9", text, StringComparison.Ordinal);
        Assert.Contains("check the identifiers", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUndescribedFailure_IsAnErrorResult_WithoutItsMessage()
    {
        await using var host = Host();

        var result = await this.Call(host, "invoices_broken");

        Assert.True(result.IsError);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;

        Assert.Contains("invoices_broken", text, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusalAtTheCall_IsAnErrorResult_SayingNotToRetry()
    {
        await using var host = Host();

        // offered when looked up, refused by the time it runs
        this.authorization.RefuseAfter("invoices_void", allowed: 1);

        var result = await this.Call(host, "invoices_void", new { id = 1 });

        Assert.True(result.IsError);
        Assert.Contains("Do not retry", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text, StringComparison.Ordinal);
    }

    public async ValueTask DisposeAsync() => await this.server.DisposeAsync();

    /// <summary>What the authorization layer was asked, and what it should refuse.</summary>
    public sealed class Counting
    {
        private readonly Dictionary<string, int> allowance = new(StringComparer.Ordinal);

        public List<string> Asked { get; } = [];

        public void Refuse(string name) => this.allowance[name] = 0;

        public void RefuseAfter(string name, int allowed) => this.allowance[name] = allowed;

        public bool Allows(string name)
        {
            this.Asked.Add(name);

            if (!this.allowance.TryGetValue(name, out var left))
            {
                return true;
            }

            this.allowance[name] = left - 1;

            return left > 0;
        }
    }

    private sealed class CountingFilter(Counting counting) : IAgentToolAuthorizationFilter
    {
        public ValueTask<AgentToolAuthorizationDecision> AuthorizeAsync(AgentToolDescriptor tool, AgentToolContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(counting.Allows(tool.Name) ? AgentToolAuthorizationDecision.Allow() : AgentToolAuthorizationDecision.Deny("Needs a supervisor."));
    }

    private sealed class Translating : IAgentToolExceptionTranslator
    {
        public AgentToolException? Translate(Exception exception, AgentToolDescriptor tool) =>
            exception is KeyNotFoundException ? new AgentToolFailedException(AgentToolFailureKind.NotFound, exception.Message, exception) : null;
    }
}
