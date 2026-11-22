using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Events;

public interface IAgentPermissionsSet : IEvent;

public sealed record AgentPermissionsSetV1(bool IsAdministrator, ImmutableArray<string> Permissions)
    : IAgentPermissionsSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.IsAdministrator = IsAdministrator;
        account.Permissions = Permissions.ToList();
        return account;
    }
}
