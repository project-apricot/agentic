using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;

namespace ApricotFramework.Agentic.Tools.Mcp.Client;

/// <summary>
/// The servers a host wrote down, connected once and shared.
/// </summary>
/// <remarks>
/// <para>
/// Covers the console and desktop case outright: a list in configuration, one connection per
/// server for the life of the process, and every caller reaching the same ones.
/// </para>
/// <para>
/// Connections are made when something first asks rather than at start-up, because a server
/// that is slow or down should delay a listing rather than a boot, and because a host may never
/// ask at all.
/// </para>
/// </remarks>
public sealed class StaticMcpClientProvider : IMcpClientProvider, IAsyncDisposable
{
    /// <summary>
    /// The servers this host connects to.
    /// </summary>
    private readonly IOptionsMonitor<McpAgentToolClientOptions> options;

    /// <summary>
    /// Where whatever caches a listing is found, when a server says its tools changed.
    /// </summary>
    /// <remarks>
    /// Resolved on use rather than injected, and that is not a style choice. The thing that
    /// caches a listing is the source, the source needs a provider to have anything to list,
    /// and taking it here would close the loop - which the container answers with a deadlock
    /// rather than an error worth reading.
    /// </remarks>
    private readonly IServiceProvider services;

    /// <summary>
    /// What a client logs to.
    /// </summary>
    private readonly ILoggerFactory loggers;

    /// <summary>
    /// Where a server that could not be reached is reported.
    /// </summary>
    private readonly ILogger<StaticMcpClientProvider> logger;

    /// <summary>
    /// The connections, once they have been made.
    /// </summary>
    private readonly ConcurrentDictionary<string, Task<McpClient?>> connections = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new instance of the provider.
    /// </summary>
    /// <param name="options">The servers this host connects to.</param>
    /// <param name="loggers">What a client logs to.</param>
    /// <param name="logger">Where a server that could not be reached is reported.</param>
    /// <param name="services">Where whatever caches a listing is found.</param>
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
    /// <param name="settings">The servers this host connects to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the connection, or null where it could not be made.</returns>
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
    /// Builds the client options, including what to do when a server says its tools changed.
    /// </summary>
    /// <returns>The options.</returns>
    private McpClientOptions Options()
    {
        var options = new McpClientOptions();

        options.Handlers.NotificationHandlers =
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
        ];

        return options;
    }

    /// <summary>
    /// Builds the way to reach a server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="loggers">What the transport logs to.</param>
    /// <returns>The transport.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server says both how to run it and where to reach it, or neither.</exception>
    private static IClientTransport Transport(McpAgentToolServer server, ILoggerFactory loggers)
    {
        if (server.Command is { Length: > 0 } && server.Endpoint is not null)
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
