using ApricotFramework.Agentic.Tools.Mcp.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.IO.Pipelines;
using System.Text.Json;

namespace ApricotFramework.Agentic.Tools.Mcp.Server.Tests;

/// <summary>
/// A tool returning a list, over a real session: an older client gets the schema and the result
/// wrapped under <c>result</c>, a client on 2026-07-28 or later gets both in their natural shape -
/// as the SDK does for its own tools.
/// </summary>
public class OutputSchemaOverTheWireTests
{
    [Theory]
    [InlineData("2025-06-18", true)]
    [InlineData("2026-07-28", false)]
    public async Task AListTool_IsShapedForTheClientsProtocolVersion(string protocolVersion, bool wrapped)
    {
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        var services = new ServiceCollection();

        services.AddAgentToolsCore();
        services.AddAgentToolType<InvoiceTools>();
        services.AddMcpServer()
            .WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream())
            .WithAgentTools();

        await using var provider = services.BuildServiceProvider();

        var server = provider.GetRequiredService<McpServer>();
        var running = server.RunAsync(TestContext.Current.CancellationToken);

        await using var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
            new McpClientOptions { ProtocolVersion = protocolVersion },
            cancellationToken: TestContext.Current.CancellationToken);

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var schema = tools.Single(tool => tool.Name == "invoices_list").ProtocolTool.OutputSchema!.Value;
        var result = await client.CallToolAsync("invoices_list", cancellationToken: TestContext.Current.CancellationToken);
        var content = result.StructuredContent!.Value;

        if (wrapped)
        {
            Assert.Equal("object", schema.GetProperty("type").GetString());
            Assert.Equal("array", schema.GetProperty("properties").GetProperty("result").GetProperty("type").GetString());
            Assert.Equal([1L, 2L], content.GetProperty("result").Deserialize<long[]>()!);
        }
        else
        {
            Assert.Equal("array", schema.GetProperty("type").GetString());
            Assert.Equal([1L, 2L], content.Deserialize<long[]>()!);
        }
    }
}
