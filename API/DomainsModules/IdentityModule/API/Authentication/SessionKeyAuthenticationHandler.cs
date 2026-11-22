using System.Security.Claims;
using System.Text.Encodings.Web;
using IdentityModule.Application.Executors;
using IdentityModule.Application.SessionKeys;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityModule.API.Authentication;

/// <summary>
/// Turns a session key into <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/>. The key only says
/// which account it is for, so the account is read on every request: a deleted account's keys stop working.
/// </summary>
public sealed class SessionKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SessionKeyProtector protector,
    AccountExecutorReader accounts)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessionKey = SessionKeyAuthentication.ReadSessionKey(Request);
        if (string.IsNullOrEmpty(sessionKey))
            return AuthenticateResult.NoResult();

        if (!protector.TryUnprotect(sessionKey, out var accountId))
            return AuthenticateResult.Fail("The session key is not valid.");

        if (await accounts.Read(accountId) is null)
            return AuthenticateResult.Fail("The session key's account does not exist.");

        var identity = new ClaimsIdentity(
            [new Claim(IdentityAuthentication.AccountIdClaim, accountId.ToString())], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
