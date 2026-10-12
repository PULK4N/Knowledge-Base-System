using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanUpdated : IEvent;

public readonly record struct FeaturePlanUpdatedV1(
    FeaturePlanId PlanId,
    string Title,
    string Content,
    FeaturePlanContentType ContentType,
    string AdditionalInfo,
    FeaturePlanStatus Status,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        var planId = PlanId;
        var plan = state.Plans.Single(item => item.Id == planId);
        plan.Title = Title;
        plan.Content = Content;
        plan.ContentType = ContentType;
        plan.AdditionalInfo = AdditionalInfo;
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
