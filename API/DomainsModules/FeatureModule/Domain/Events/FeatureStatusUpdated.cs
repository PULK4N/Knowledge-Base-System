using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureStatusUpdated : IEvent;

public readonly record struct FeatureStatusUpdatedV1(
    string Status
) : IFeatureStatusUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Status = Status;
        return state;
    }
}

public readonly record struct FeatureStatusUpdatedV2(
    string Status,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureStatusUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Status = Status;
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
