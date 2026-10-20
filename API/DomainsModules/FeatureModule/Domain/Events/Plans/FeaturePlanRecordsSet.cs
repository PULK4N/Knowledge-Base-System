using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanRecordsSet : IEvent;

/// <summary>
/// Replaces the records a plan links to. An empty list removes every link.
/// </summary>
public readonly record struct FeaturePlanRecordsSetV1(
    FeaturePlanId PlanId,
    ImmutableArray<FeatureRecordId> RecordIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanRecordsSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.RecordIds = RecordIds.ToList();
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
