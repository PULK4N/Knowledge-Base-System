using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;

namespace FeatureModule.Persistence;

public sealed class FeatureSearchProjector(
    FeatureSearchRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(
            stateInfos
                .Where(
                    stateInfo => stateInfo.StateData is FeatureStateData
                )
                .Select(
                    stateInfo => new FeatureSearchUpdate(
                        (FeatureStateData)stateInfo.StateData,
                        stateInfo.CurrentOrderNumber
                    )
                )
                .ToList()
        );
}
