using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceAutoLoadUpdated : IEvent;

public readonly record struct SkillReferenceAutoLoadUpdatedV1(
    string RelativePath,
    bool LoadAutomatically
) : ISkillReferenceAutoLoadUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        var reference = state.References[RelativePath];
        state.References[RelativePath] = reference with
        {
            LoadAutomatically = LoadAutomatically
        };

        return state;
    }
}
