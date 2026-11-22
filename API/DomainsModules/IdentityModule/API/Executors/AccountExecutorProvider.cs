using ActionModule.Shared;
using ActionModule.Shared.Models;
using IdentityModule.API.Authentication;
using IdentityModule.Application;
using IdentityModule.Application.Executors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace IdentityModule.API.Executors;

/// <summary>
/// Gives each request the executor of the account its login cookie or session key names. A request
/// without a valid login gets <paramref name="anonymous"/>'s executor, which has no administrator flag or
/// permissions; with authentication disabled it runs as the administrator instead.
/// </summary>
public sealed class AccountExecutorProvider(
    IHttpContextAccessor httpContextAccessor,
    AccountExecutorReader reader,
    TemporaryExecutorProvider anonymous,
    IOptions<IdentityAuthenticationSettings> settings)
    : IExecutorProvider
{
    public async Task<Executor> GetExecutor()
    {
        var accountId = httpContextAccessor.HttpContext?.User
            .FindFirst(IdentityAuthentication.AccountIdClaim)?.Value;

        if (Guid.TryParse(accountId, out var id) && await reader.Read(id) is { } executor)
            return executor;

        if (!settings.Value.Enabled && await reader.Read(IdentityDefaults.AdministratorId.Value) is { } administrator)
            return administrator;

        return await anonymous.GetExecutor();
    }
}
