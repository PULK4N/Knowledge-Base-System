using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;

namespace PolicyModule.Persistence;

public sealed class PolicyProjectSummaryProjector(
    PolicyProjectSummaryRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Replace(
            stateInfos
                .Select(stateInfo => stateInfo.StateData)
                .OfType<ProjectPoliciesStateData>()
                .ToList()
        );
}
