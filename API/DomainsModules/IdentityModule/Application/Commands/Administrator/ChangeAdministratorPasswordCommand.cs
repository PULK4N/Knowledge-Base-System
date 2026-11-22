using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using IdentityModule.Domain;
using IdentityModule.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace IdentityModule.Application.Commands;

public sealed class ChangeAdministratorPasswordCommand(
    StateMachineHandler handler,
    IPasswordHasher<AccountStateData> hasher)
    : Command<bool>
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    private AccountStateData? _administrator;

    public override async Task<bool> CanExecute(Executor executor)
    {
        _administrator = (await handler.GetByAggregateId(IdentityDefaults.AdministratorId))?.StateData
            as AccountStateData;
        return _administrator is not null
               && hasher.VerifyHashedPassword(_administrator, _administrator.PasswordHash, CurrentPassword)
               != PasswordVerificationResult.Failed
               && IdentityDefaults.IsValidNewPassword(NewPassword, ConfirmPassword);
    }

    protected override async Task<bool> ExecuteInternal(Executor executor)
    {
        await handler.ExecuteEvents(EventPayload.Create(executor.Id, IdentityDefaults.AdministratorId,
            IdentityDefaults.StateMachineId,
            new AdministratorPasswordChangedV1(hasher.HashPassword(_administrator!, NewPassword))));
        return true;
    }
}
