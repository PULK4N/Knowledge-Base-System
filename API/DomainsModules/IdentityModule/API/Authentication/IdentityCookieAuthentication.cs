using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityModule.API.Authentication;

/// <summary>
/// The administrator's login cookie. It holds only the account ID; everything else about the account
/// is read from the account itself.
/// </summary>
public static class IdentityCookieAuthentication
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string CookieName = "kb.session";

    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    public static AuthenticationBuilder AddIdentityCookie(this AuthenticationBuilder builder) =>
        builder.AddCookie(Scheme, options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                // The web app is also served over plain HTTP on localhost, where a Secure cookie is dropped.
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = Lifetime;
                options.SlidingExpiration = true;
                // This is an API: answer with status codes instead of redirecting to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

    public static Task SignInAccount(this HttpContext httpContext, Guid accountId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(IdentityAuthentication.AccountIdClaim, accountId.ToString())], Scheme);
        // Persistent, so the cookie outlives a browser restart for its sliding lifetime.
        return httpContext.SignInAsync(Scheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    public static Task SignOutAccount(this HttpContext httpContext) =>
        httpContext.SignOutAsync(Scheme);
}
