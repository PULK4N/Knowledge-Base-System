using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanStatusChanged : IEvent;

/// <summary>
/// Changes only the plan status so callers do not resend the plan content.
/// </summary>
public readonly record struct FeaturePlanStatusChangedV1(
    FeaturePlanId PlanId,
    FeaturePlanStatus Status,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanStatusChanged
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.Status = Status;
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
