using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureBranchSet : IEvent;

public readonly record struct FeatureBranchSetV1(
    string BranchName,
    string Description,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureBranchSet
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (FeatureStateData)stateData;
        state.Branch = new FeatureBranch
        {
            Name = BranchName,
            Description = Description
        };
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
