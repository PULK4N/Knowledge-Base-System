using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

// Released plan events still read and clear the legacy current-plan selection during replay.
#pragma warning disable CS0618

public interface IFeaturePlanRemoved : IEvent;

/// <summary>
/// Removes a plan and clears the current selection when that plan was current.
/// </summary>
public readonly record struct FeaturePlanRemovedV1(
    FeaturePlanId PlanId
) : IFeaturePlanRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        state.Plans.Remove(plan);

        if (state.CurrentPlanId == PlanId)
            state.CurrentPlanId = null;

        return state;
    }
}

public readonly record struct FeaturePlanRemovedV2(
    FeaturePlanId PlanId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        state.Plans.Remove(plan);

        if (state.CurrentPlanId == PlanId)
            state.CurrentPlanId = null;

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
