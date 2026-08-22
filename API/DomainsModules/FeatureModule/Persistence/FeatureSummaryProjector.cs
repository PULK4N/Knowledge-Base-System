using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;

namespace FeatureModule.Persistence;

public sealed class FeatureSummaryProjector(
    FeatureSummaryRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(
            stateInfos
                .Select(stateInfo => stateInfo.StateData)
                .OfType<FeatureStateData>()
                .ToList()
        );
}
