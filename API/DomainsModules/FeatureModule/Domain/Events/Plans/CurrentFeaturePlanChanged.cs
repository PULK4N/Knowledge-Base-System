using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

// Released plan events still read and clear the legacy current-plan selection during replay.
#pragma warning disable CS0618

public interface ICurrentFeaturePlanChanged : IEvent;

[Obsolete("Replay only. Current-plan selection was removed.")]
public readonly record struct CurrentFeaturePlanChangedV1(
    FeaturePlanId PlanId
) : ICurrentFeaturePlanChanged
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.CurrentPlanId = PlanId;
        return state;
    }
}

[Obsolete("Replay only. Current-plan selection was removed.")]
public readonly record struct CurrentFeaturePlanChangedV2(
    FeaturePlanId PlanId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : ICurrentFeaturePlanChanged
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.CurrentPlanId = PlanId;
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
