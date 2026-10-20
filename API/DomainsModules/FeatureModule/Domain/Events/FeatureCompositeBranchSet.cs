using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureCompositeBranchSet : IEvent;

public readonly record struct FeatureCompositeBranchSetV1(
    string? BranchName,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureCompositeBranchSet
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        state.CompositeBranchName = BranchName;
        state.MemoryHistory.Add(new MemoryHistoryRecord(
            eventExecutionInfo.EventName,
            eventExecutionInfo.Timestamp,
            MemoryAggregateId
        ));
        return state;
    }
}
