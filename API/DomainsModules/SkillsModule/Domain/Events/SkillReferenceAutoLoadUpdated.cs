using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

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

public readonly record struct SkillReferenceAutoLoadUpdatedV2(
    string RelativePath,
    bool LoadAutomatically,
    Guid SessionId,
    AggregateId MemoryAggregateId
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
        state.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return state;
    }
}
