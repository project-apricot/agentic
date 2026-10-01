using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// Validates the host's code-registered tools at startup.
/// </summary>
/// <remarks>External sources are not checked: they need a caller and should not block startup (see <c>AgentToolCuration.DropRejected</c>).</remarks>
/// <param name="scopes">Source of the scope the tools are described in.</param>
public sealed class AgentToolStartupValidation(IServiceScopeFactory scopes) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();

        var source = scope.ServiceProvider.GetServices<IAgentToolSource>().OfType<RegistrationAgentToolSource>().FirstOrDefault();

        if (source is null)
        {
            return;
        }

        var context = await scope.ServiceProvider.GetRequiredService<IAgentToolContextFactory>().CreateAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

        var validators = scope.ServiceProvider.GetServices<IAgentToolValidator>().ToList();

        var declared = new HashSet<string>(StringComparer.Ordinal);

        foreach (var tool in await source.GetToolsAsync(context, cancellationToken).ConfigureAwait(false))
        {
            foreach (var validator in validators)
            {
                validator.Validate(tool);
            }

            if (!declared.Add(tool.Name))
            {
                throw new Exceptions.AgentToolDeclarationException(
                    $"Two tools are offered as '{tool.Name}'. A tool name is a contract and has to address one operation.");
            }
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
