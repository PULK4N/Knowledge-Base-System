using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Application.DTOs;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Commands;

/// <summary>
/// Creates the administrator account on first login, replacing the initial <c>admin</c> / <c>123</c>
/// credentials. Only the password hash is stored.
/// </summary>
public sealed class SetInitialCredentialsCommand(
    StateMachineHandler handler,
    IPasswordHasher<AccountStateData> hasher)
    : Command<LoginResultDto>
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string? NewUserName { get; set; }
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public override async Task<bool> CanExecute(Executor executor) =>
        CurrentPassword == IdentityDefaults.InitialPassword
        && IdentityDefaults.IsValidNewPassword(NewPassword, ConfirmPassword)
        && await handler.GetByAggregateId(IdentityDefaults.AdministratorId) is null;

    protected override async Task<LoginResultDto> ExecuteInternal(Executor executor)
    {
        var userName = string.IsNullOrWhiteSpace(NewUserName)
            ? IdentityDefaults.InitialUserName
            : NewUserName.Trim();
        var administrator = new AccountStateData(IdentityDefaults.AdministratorId) { UserName = userName };

        await handler.ExecuteEvents(EventPayload.Create(executor.Id, IdentityDefaults.AdministratorId,
            IdentityDefaults.StateMachineId,
            new AdministratorCreatedV1(userName, hasher.HashPassword(administrator, NewPassword))));
        return new LoginResultDto(true, MustChangePassword: false);
    }
}
