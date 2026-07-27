using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillDeleted : IEvent;

public sealed class SkillDeletedV1 : ISkillDeleted
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;

        state.IsDeleted = true;

        return state;
    }
}
