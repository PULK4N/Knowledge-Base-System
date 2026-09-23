using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureRemoved : IEvent;

public readonly record struct FeatureRemovedV1 : IFeatureRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.IsDeleted = true;
        return state;
    }
}

public readonly record struct FeatureRemovedV2(
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.IsDeleted = true;
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
