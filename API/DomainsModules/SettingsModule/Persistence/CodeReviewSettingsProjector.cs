using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SettingsModule.Domain;

namespace SettingsModule.Persistence;

public sealed class CodeReviewSettingsProjector(CodeReviewSettingsRepository repository) : IProjector
{
    public Task Update(List<StateInfo> stateInfos) => repository.Write(
        stateInfos.Select(value => value.StateData).OfType<SettingsStateData>().ToList());
}
