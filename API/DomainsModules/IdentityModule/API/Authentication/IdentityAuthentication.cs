using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityModule.API.Authentication;

/// <summary>
/// The default authentication scheme. It does not read anything itself: it hands each request to the
/// session key handler when it carries <c>Authorization: Bearer</c>, and to the login cookie otherwise.
/// Both put the same account ID claim on <see cref="HttpContext.User"/>.
/// </summary>
public static class IdentityAuthentication
{
    public const string Scheme = "KnowledgeBase";
    public const string AccountIdClaim = ClaimTypes.NameIdentifier;

    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(Scheme)
            .AddPolicyScheme(Scheme, Scheme, options =>
                options.ForwardDefaultSelector = context =>
                    SessionKeyAuthentication.HasBearerHeader(context.Request)
                        ? SessionKeyAuthentication.Scheme
                        : IdentityCookieAuthentication.Scheme)
            .AddIdentityCookie()
            .AddSessionKey();

        return services;
    }
}
