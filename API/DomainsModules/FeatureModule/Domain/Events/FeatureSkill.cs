using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>
/// Feature-skill links are retired in favour of project-skill links. The
/// events stay so existing streams replay; nothing writes them any more.
/// </summary>
public static class FeatureSkillLinks
{
    public const string Retired =
        "Feature-skill links are retired; kept only to replay existing events.";
}

[Obsolete(FeatureSkillLinks.Retired)]
public interface IFeatureSkillAdded : IEvent;

[Obsolete(FeatureSkillLinks.Retired)]
public readonly record struct FeatureSkillAddedV1(
    AggregateId SkillId
) : IFeatureSkillAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Add(SkillId);
        return state;
    }
}

[Obsolete(FeatureSkillLinks.Retired)]
public interface IFeatureSkillRemoved : IEvent;

[Obsolete(FeatureSkillLinks.Retired)]
public readonly record struct FeatureSkillRemovedV1(
    AggregateId SkillId
) : IFeatureSkillRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Remove(SkillId);
        return state;
    }
}

[Obsolete(FeatureSkillLinks.Retired)]
public readonly record struct FeatureSkillAddedV2(
    AggregateId SkillId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureSkillAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Add(SkillId);
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

[Obsolete(FeatureSkillLinks.Retired)]
public readonly record struct FeatureSkillRemovedV2(
    AggregateId SkillId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureSkillRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.RelatedSkillIds.Remove(SkillId);
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
