using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ApricotFramework.Agentic.Examples.Web.Auth;

/// <summary>
/// Builds a principal from request headers, so the example needs no token issuer.
/// </summary>
/// <remarks>
/// Send <c>X-Demo-Subject</c> and a space separated <c>X-Demo-Scopes</c>. Sending neither is how
/// an anonymous caller is demonstrated, which is worth trying against the tool listing: it comes
/// back empty rather than refused, because nothing the caller cannot invoke is offered.
/// </remarks>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
public sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme name.</summary>
    public const string SchemeName = "Demo";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = this.Request.Headers["X-Demo-Subject"].ToString();
        var scopes = this.Request.Headers["X-Demo-Scopes"].ToString();

        if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(scopes))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new("sub", string.IsNullOrWhiteSpace(subject) ? "anonymous" : subject) };

        // one claim per scope, so RequireClaim("scope", "tickets.read") matches
        claims.AddRange(scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(scope => new Claim("scope", scope)));

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", ClaimTypes.Role);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
