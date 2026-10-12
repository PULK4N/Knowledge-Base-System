using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanPreviousPlanSet : IEvent;

/// <summary>
/// Sets the plan done before this one, or clears it so the plan's base branch
/// comes from the feature branch.
/// </summary>
public readonly record struct FeaturePlanPreviousPlanSetV1(
    FeaturePlanId PlanId,
    FeaturePlanId? PreviousPlanId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanPreviousPlanSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.PreviousPlanId = PreviousPlanId;
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
