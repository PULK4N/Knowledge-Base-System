using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureSummaryUpdated : IEvent;

public readonly record struct FeatureSummaryUpdatedV1(
    string Summary
) : IFeatureSummaryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Summary = Summary;
        return state;
    }
}

public readonly record struct FeatureSummaryUpdatedV2(
    string Summary,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureSummaryUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Summary = Summary;
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
