using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Events;

public interface IAdministratorCreated : IEvent;

public sealed record AdministratorCreatedV1(string UserName, string PasswordHash) : IAdministratorCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.IsCreated = true;
        account.IsAdministrator = true;
        account.UserName = UserName;
        account.DisplayName = UserName;
        account.PasswordHash = PasswordHash;
        return account;
    }
}
