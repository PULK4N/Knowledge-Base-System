using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

// Released plan events still read and clear the legacy current-plan selection during replay.
#pragma warning disable CS0618

public interface ICurrentFeaturePlanUpdated : IEvent;

[Obsolete("Replay only. Current-plan selection was removed; use FeaturePlanUpdatedV1.")]
public readonly record struct CurrentFeaturePlanUpdatedV1(
    string Title,
    string Content,
    FeaturePlanContentType ContentType
) : ICurrentFeaturePlanUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var plan = state.Plans.Single(
            item => item.Id == state.CurrentPlanId!.Value
        );
        plan.Title = Title;
        plan.Content = Content;
        plan.ContentType = ContentType;
        plan.UpdatedAt = eventExecutionInfo.Timestamp;
        return state;
    }
}

[Obsolete("Replay only. Current-plan selection was removed; use FeaturePlanUpdatedV1.")]
public readonly record struct CurrentFeaturePlanUpdatedV2(
    string Title,
    string Content,
    FeaturePlanContentType ContentType,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ICurrentFeaturePlanUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var plan = state.Plans.Single(
            item => item.Id == state.CurrentPlanId!.Value
        );
        plan.Title = Title;
        plan.Content = Content;
        plan.ContentType = ContentType;
        plan.UpdatedAt = eventExecutionInfo.Timestamp;
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
