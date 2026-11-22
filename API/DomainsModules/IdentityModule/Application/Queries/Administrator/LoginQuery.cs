using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Queries;

/// <summary>
/// Checks the administrator's username and password. While the seeded administrator has no password
/// hash, only the initial <c>admin</c> / <c>123</c> pair is accepted and must be changed.
/// </summary>
public sealed class LoginQuery(StateMachineHandler handler, IPasswordHasher<AccountStateData> hasher)
    : Query<LoginResultDto>
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    protected override async Task<LoginResultDto> ExecuteInternal(Executor executor)
    {
        var administrator = (await handler.GetByAggregateId(IdentityDefaults.AdministratorId))?.StateData
            as AccountStateData;
        return administrator is not null
               && IdentityDefaults.UserNamesMatch(UserName, administrator.UserName)
               && IdentityDefaults.PasswordMatches(hasher, administrator, Password)
            ? new LoginResultDto(true, IdentityDefaults.MustChangePassword(administrator))
            : LoginResultDto.Failed;
    }
}
