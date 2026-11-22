using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace IdentityModule.API.Authentication;

/// <summary>
/// The agents' login: a session key sent as <c>Authorization: Bearer &lt;key&gt;</c>.
/// </summary>
public static class SessionKeyAuthentication
{
    public const string Scheme = "SessionKey";

    private const string BearerPrefix = "Bearer ";

    public static AuthenticationBuilder AddSessionKey(this AuthenticationBuilder builder) =>
        builder.AddScheme<AuthenticationSchemeOptions, SessionKeyAuthenticationHandler>(Scheme, null);

    public static bool HasBearerHeader(HttpRequest request) =>
        request.Headers.Authorization.ToString().StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase);

    public static string? ReadSessionKey(HttpRequest request) =>
        HasBearerHeader(request) ? request.Headers.Authorization.ToString()[BearerPrefix.Length..].Trim() : null;
}
