using ApricotFramework.Agentic.Tools.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApricotFramework.Agentic.Tools.Registry;

/// <summary>
/// Looks over the tools this host declared, before it starts serving.
/// </summary>
/// <remarks>
/// <para>
/// A malformed declaration of your own should stop the host, not surface to whoever happens to
/// ask first. That was free while the registry composed once; now that it composes per call,
/// something has to do it deliberately.
/// </para>
/// <para>
/// Only the tools registered in code are checked. A source reaching an upstream server cannot be
/// consulted without a caller and should not be reached at start-up anyway - a service that will
/// not start over a third party's mistake is worse than one running with fewer tools, which is
/// what <c>AgentToolCuration.DropRejected</c> is for.
/// </para>
/// </remarks>
/// <param name="scopes">Where the scope to describe the tools in comes from.</param>
/// <param name="contexts">How this host decides who is asking.</param>
public sealed class AgentToolStartupValidation(IServiceScopeFactory scopes, IAgentToolContextFactory contexts) : IHostedService
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

        var context = await contexts.CreateAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);

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
