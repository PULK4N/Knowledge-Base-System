using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Events;

public interface IAdministratorCreated : IEvent;

/// <summary>
/// Written only by the seed migration. The administrator starts without a password hash, which
/// means the initial password is still in use and must be changed.
/// </summary>
public sealed record AdministratorCreatedV1(string UserName) : IAdministratorCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.IsCreated = true;
        account.IsAdministrator = true;
        account.UserName = UserName;
        account.DisplayName = UserName;
        return account;
    }
}
