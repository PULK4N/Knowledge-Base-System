using ActionModule.Shared;
using ActionModule.Shared.Models;
using IdentityModule.API.Authentication;
using IdentityModule.Application.Executors;
using Microsoft.AspNetCore.Http;

namespace IdentityModule.API.Executors;

/// <summary>
/// Gives each request the executor of the account in its login cookie. A request without a valid
/// login gets <paramref name="anonymous"/>'s executor, which has no administrator flag or permissions.
/// </summary>
public sealed class AccountExecutorProvider(
    IHttpContextAccessor httpContextAccessor,
    AccountExecutorReader reader,
    TemporaryExecutorProvider anonymous)
    : IExecutorProvider
{
    public async Task<Executor> GetExecutor()
    {
        var accountId = httpContextAccessor.HttpContext?.User
            .FindFirst(IdentityCookieAuthentication.AccountIdClaim)?.Value;

        return Guid.TryParse(accountId, out var id) && await reader.Read(id) is { } executor
            ? executor
            : await anonymous.GetExecutor();
    }
}
