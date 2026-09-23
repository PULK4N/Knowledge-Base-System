using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain.Events;

public interface ISkillReferenceUpdated : IEvent;

public readonly record struct SkillReferenceUpdatedV1(
    string RelativePath,
    string Content
) : ISkillReferenceUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        if (state.References.ContainsKey(RelativePath))
        {
            state.References[RelativePath] = new SkillReference2(Content);
        }

        return state;
    }
}

public readonly record struct SkillReferenceUpdatedV2(
    string RelativePath,
    string Content,
    bool LoadAutomatically
) : ISkillReferenceUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References[RelativePath] = new SkillReference2(
            Content,
            LoadAutomatically
        );

        return state;
    }
}

public readonly record struct SkillReferenceUpdatedV3(
    string RelativePath,
    string Content,
    bool LoadAutomatically,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ISkillReferenceUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (SkillStateData)stateData;
        state.References[RelativePath] = new SkillReference2(
            Content,
            LoadAutomatically
        );
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
