using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceDeleted : IEvent
{
    string RelativePath { get; }
}

public sealed class SkillReferenceDeletedV1 : ISkillReferenceDeleted
{
    public required string RelativePath { get; init; }

    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References.Remove(RelativePath);

        return state;
    }
}
