using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Persistence;

public sealed class PolicyProjectSummaryProjector(
    PolicyProjectSummaryRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(
            stateInfos
                .Select(stateInfo => stateInfo.StateData)
                .OfType<ProjectStateData>()
                .ToList()
        );
}
