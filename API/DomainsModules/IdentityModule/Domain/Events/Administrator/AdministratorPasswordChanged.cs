using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Events;

public interface IAdministratorPasswordChanged : IEvent;

public sealed record AdministratorPasswordChangedV1(string PasswordHash) : IAdministratorPasswordChanged
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.PasswordHash = PasswordHash;
        return account;
    }
}
