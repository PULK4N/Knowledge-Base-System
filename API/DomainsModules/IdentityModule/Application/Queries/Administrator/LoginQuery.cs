using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Queries;

/// <summary>
/// Checks the administrator's username and password. Before the administrator has set their own
/// credentials, only the initial <c>admin</c> / <c>123</c> pair is accepted and must be changed.
/// </summary>
public sealed class LoginQuery(StateMachineHandler handler, IPasswordHasher<AccountStateData> hasher)
    : Query<LoginResultDto>
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    protected override async Task<LoginResultDto> ExecuteInternal(Executor executor)
    {
        var stateInfo = await handler.GetByAggregateId(IdentityDefaults.AdministratorId);
        if (stateInfo is null)
            return IdentityDefaults.UserNamesMatch(UserName, IdentityDefaults.InitialUserName)
                   && Password == IdentityDefaults.InitialPassword
                ? new LoginResultDto(true, MustChangePassword: true)
                : LoginResultDto.Failed;

        var administrator = (AccountStateData)stateInfo.StateData;
        var passwordMatches = hasher.VerifyHashedPassword(administrator, administrator.PasswordHash, Password)
            != PasswordVerificationResult.Failed;
        return IdentityDefaults.UserNamesMatch(UserName, administrator.UserName) && passwordMatches
            ? new LoginResultDto(true, MustChangePassword: false)
            : LoginResultDto.Failed;
    }
}
