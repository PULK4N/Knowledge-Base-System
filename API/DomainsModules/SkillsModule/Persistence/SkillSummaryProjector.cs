using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain;

namespace SkillsModule.Persistence;

public sealed class SkillSummaryProjector(
    SkillSummaryRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(
            stateInfos
                .Select(stateInfo => stateInfo.StateData)
                .OfType<SkillStateData>()
                .ToList()
        );
}
