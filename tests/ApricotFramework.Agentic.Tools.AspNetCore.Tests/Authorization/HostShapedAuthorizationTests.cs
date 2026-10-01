using System.ComponentModel;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ApricotFramework.Agentic.Tools.AspNetCore.Extensions;
using ApricotFramework.Agentic.Tools.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Agentic.Tools.AspNetCore.Tests.Authorization;

/// <summary>
/// Authorization in a host shaped like a real one: a scoped handler reading per-request state,
/// scope validation on, and a caller taken from the request.
/// </summary>
public class HostShapedAuthorizationTests
{
    private static ServiceProvider Host(Action<IServiceCollection>? more = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthorizationCore();
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, OtherScheme>(OtherScheme.Name, _ => { });

        // scoped, the way a handler resolving a caller's grants per request is
        services.AddScoped<Grants>();
        services.AddScoped<IAuthorizationHandler, GrantHandler>();

        services.AddAgentToolsWeb().WithAuthorization();
        services.AddAgentToolType<GatedTools>();

        more?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static ClaimsPrincipal Caller(params string[] grants) =>
        new(new ClaimsIdentity([new Claim("sub", "u1"), .. grants.Select(grant => new Claim("grant", grant))], "test"));

    // the request's services are the request's scope, as ASP.NET Core arranges them
    private static AsyncServiceScope Request(ServiceProvider host, ClaimsPrincipal? user)
    {
        var request = host.CreateAsyncScope();

        host.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity()),
            RequestServices = request.ServiceProvider
        };

        return request;
    }

    private static async Task<string> Call(ServiceProvider host, string tool, ClaimsPrincipal? user)
    {
        await using var request = Request(host, user);

        return await host.GetRequiredService<IAgentToolExecutor>().InvokeCompleteAsync(tool, null, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AScopedAuthorizationHandler_BuildsUnderScopeValidation_AndDecides()
    {
        await using var host = Host();

        Assert.Equal("\"read\"", await Call(host, "gated_read", Caller("items:read")));

        await Assert.ThrowsAsync<AgentToolAccessDeniedException>(() => Call(host, "gated_read", Caller("items:other")));
    }

    [Fact]
    public async Task ARefusal_CarriesWhatTheHandlerSaidIsMissing()
    {
        await using var host = Host();

        var exception = await Assert.ThrowsAsync<AgentToolAccessDeniedException>(() => Call(host, "gated_read", Caller()));

        Assert.IsNotType<AgentToolUnauthenticatedException>(exception);
        Assert.Contains("items:read", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NobodyCalling_IsUnauthenticated_NotDenied()
    {
        await using var host = Host();

        await Assert.ThrowsAsync<AgentToolUnauthenticatedException>(() => Call(host, "gated_read", null));
    }

    [Fact]
    public async Task AToolDeclaringNothing_IsOpen_EvenToNobody()
    {
        await using var host = Host();

        Assert.Equal("\"open\"", await Call(host, "open_read", null));
        Assert.Equal("\"open\"", await Call(host, "open_read", Caller()));
    }

    [Fact]
    public async Task AToolNamingAScheme_IsDecidedOnThatSchemesIdentity()
    {
        await using var host = Host();

        // the request's own identity holds nothing; the scheme the tool names grants what it needs
        Assert.Equal("\"other\"", await Call(host, "gated_other", Caller()));
    }

    [Fact]
    public async Task TheListing_OffersOnlyWhatTheCallerMayUse()
    {
        await using var host = Host();

        await using var request = Request(host, Caller("items:read"));

        var offered = await host.GetRequiredService<IAgentToolExecutor>().GetAvailableToolsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["gated_other", "gated_read", "open_read"], offered.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [AgentToolType]
    private sealed class GatedTools
    {
        [AgentTool("gated_read", Title = "Read", ReadOnly = true)]
        [RequireGrant("items:read")]
        [Description("Reads.")]
        public static string Read() => "read";

        [AgentTool("gated_other", Title = "Other", ReadOnly = true)]
        [RequireGrant("items:other-scheme", AuthenticationSchemes = OtherScheme.Name)]
        [Description("Reads through another scheme.")]
        public static string Other() => "other";

        [AgentTool("open_read", Title = "Open", ReadOnly = true)]
        [Description("Reads something anyone may read.")]
        public static string Open() => "open";
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class RequireGrantAttribute(string grant) : AuthorizeAttribute, IAuthorizationRequirementData
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new GrantRequirement(grant)];
    }

    private sealed record GrantRequirement(string Grant) : IAuthorizationRequirement;

    private sealed class Grants(IHttpContextAccessor accessor)
    {
        public bool Holds(ClaimsPrincipal user, string grant) =>
            accessor.HttpContext is not null && user.HasClaim("grant", grant);
    }

    private sealed class GrantHandler(Grants grants) : AuthorizationHandler<GrantRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GrantRequirement requirement)
        {
            if (grants.Holds(context.User, requirement.Grant))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, $"Requires the {requirement.Grant} access."));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class OtherScheme(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "other";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u1"), new Claim("grant", "items:other-scheme")], Name)), Name)));
    }
}
