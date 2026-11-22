using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using IdentityModule.Domain;

namespace IdentityModule.Persistence;

public sealed class AccountListProjector(AccountListRepository repository) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(stateInfos
            .Where(stateInfo => stateInfo.StateData is AccountStateData)
            .Select(stateInfo => new AccountListUpdate((AccountStateData)stateInfo.StateData,
                stateInfo.CurrentOrderNumber, stateInfo.LastUpdateTimestamp))
            .ToList());
}
