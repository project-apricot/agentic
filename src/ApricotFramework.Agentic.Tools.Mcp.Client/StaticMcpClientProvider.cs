using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// Connects once to configured MCP servers and shares the connections.
/// </summary>
/// <remarks>
/// Connections are made on first use, so a slow or down server delays a listing rather than start-up.
/// </remarks>
public sealed class StaticMcpClientProvider : IMcpClientProvider, IAsyncDisposable
{
    /// <summary>
    /// The configured servers.
    /// </summary>
    private readonly IOptionsMonitor<McpAgentToolClientOptions> options;

    /// <summary>
    /// Resolves listing caches to invalidate when a server's tools change.
    /// </summary>
    /// <remarks>
    /// Resolved on use, not injected, to avoid a dependency cycle with the source.
    /// </remarks>
    private readonly IServiceProvider services;

    /// <summary>
    /// The client logger factory.
    /// </summary>
    private readonly ILoggerFactory loggers;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<StaticMcpClientProvider> logger;

    /// <summary>
    /// The connections, per server.
    /// </summary>
    private readonly ConcurrentDictionary<string, Task<McpClient?>> connections = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new instance of the provider.
    /// </summary>
    /// <param name="options">The configured servers.</param>
    /// <param name="loggers">The client logger factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="services">The service provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public StaticMcpClientProvider(
        IOptionsMonitor<McpAgentToolClientOptions> options,
        ILoggerFactory loggers,
        ILogger<StaticMcpClientProvider> logger,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggers);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(services);

        this.options = options;
        this.loggers = loggers;
        this.logger = logger;
        this.services = services;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<McpClient>> GetClientsAsync(IAgentToolSourceContext context, CancellationToken cancellationToken = default)
    {
        var settings = this.options.CurrentValue;

        var reached = new List<McpClient>(settings.Servers.Count);

        foreach (var server in settings.Servers)
        {
            if (await this.ConnectAsync(server, settings, cancellationToken).ConfigureAwait(false) is { } client)
            {
                reached.Add(client);
            }
        }

        return reached;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var pending in this.connections.Values)
        {
            try
            {
                if (await pending.ConfigureAwait(false) is { } client)
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // a connection that failed to open has nothing to close, and a host shutting
                // down should not be stopped by a third party it could not reach
            }
        }

        this.connections.Clear();
    }

    /// <summary>
    /// Connects to one server, once.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="settings">The client options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection, or null if it could not be made.</returns>
    private Task<McpClient?> ConnectAsync(McpAgentToolServer server, McpAgentToolClientOptions settings, CancellationToken cancellationToken)
    {
        return this.connections.GetOrAdd(server.Name, _ => OpenAsync());

        async Task<McpClient?> OpenAsync()
        {
            try
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                deadline.CancelAfter(settings.ConnectTimeout);

                return await McpClient.CreateAsync(Transport(server, this.loggers), this.Options(), this.loggers, deadline.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                McpAgentToolLog.Unreachable(this.logger, exception, server.Name);

                if (settings.RequireEveryServer)
                {
                    throw;
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Builds the client options, including the tools-changed handler.
    /// </summary>
    /// <returns>The options.</returns>
    private McpClientOptions Options()
    {
        var clientOptions = new McpClientOptions
        {
            Handlers =
            {
                NotificationHandlers =
                [
                    new KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>(
                        NotificationMethods.ToolListChangedNotification,
                        (_, _) =>
                        {
                            // which connection said so is not carried on the notification, so every
                            // listing is dropped. asking each server once more is cheap beside offering
                            // a model a tool that no longer exists
                            (this.services.GetService(typeof(IMcpToolInvalidation)) as IMcpToolInvalidation)?.Invalidate();

                            return ValueTask.CompletedTask;
                        })
                ]
            }
        };

        return clientOptions;
    }

    /// <summary>
    /// Builds the transport for a server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="loggers">The logger factory.</param>
    /// <returns>The transport.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server specifies both a command and an endpoint, or neither.</exception>
    private static IClientTransport Transport(McpAgentToolServer server, ILoggerFactory loggers)
    {
        if (server is { Command.Length: > 0, Endpoint: not null })
        {
            throw new InvalidOperationException($"The MCP server '{server.Name}' says both how to run it and where to reach it. It can be one or the other.");
        }

        if (server.Command is { Length: > 0 } command)
        {
            return new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = server.Name,
                    Command = command,
                    Arguments = [.. server.Arguments],
                    EnvironmentVariables = server.Environment.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
                },
                loggers);
        }

        if (server.Endpoint is { } endpoint)
        {
            return new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Name = server.Name,
                    Endpoint = endpoint,
                    AdditionalHeaders = server.Headers.ToDictionary(entry => entry.Key, entry => entry.Value ?? string.Empty, StringComparer.Ordinal)
                },
                loggers);
        }

        throw new InvalidOperationException($"The MCP server '{server.Name}' says neither how to run it nor where to reach it.");
    }
}
