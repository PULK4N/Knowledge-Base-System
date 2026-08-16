using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceDeleted : IEvent;

public readonly record struct SkillReferenceDeletedV1(
    string RelativePath
) : ISkillReferenceDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References.Remove(RelativePath);

        return state;
    }
}
