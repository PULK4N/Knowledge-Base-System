using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain;

namespace SkillsModule.Persistence;

public sealed class SkillListProjector(
    SkillListRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) =>
        repository.Write(
            stateInfos
                .Where(stateInfo => stateInfo.StateData is SkillStateData)
                .Select(
                    stateInfo => new SkillListUpdate(
                        (SkillStateData)stateInfo.StateData,
                        stateInfo.CurrentOrderNumber
                    )
                )
                .ToList()
        );
}
