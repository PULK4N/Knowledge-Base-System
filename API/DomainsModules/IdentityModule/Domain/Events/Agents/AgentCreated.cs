using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Events;

public interface IAgentCreated : IEvent;

public sealed record AgentCreatedV1(
    string UserName,
    string DisplayName,
    bool IsAdministrator,
    ImmutableArray<string> Permissions
) : IAgentCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var account = (AccountStateData)stateData;
        account.IsCreated = true;
        account.IsAgent = true;
        account.UserName = UserName;
        account.DisplayName = DisplayName;
        account.IsAdministrator = IsAdministrator;
        account.Permissions = Permissions.ToList();
        return account;
    }
}
