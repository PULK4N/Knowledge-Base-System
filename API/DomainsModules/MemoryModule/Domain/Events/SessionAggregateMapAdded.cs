using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface ISessionAggregateMapAdded : IEvent;

public sealed record SessionAggregateMapAddedV1(
    ThreadId ThreadId,
    AggregateId MemoryAggregateId
) : ISessionAggregateMapAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (SessionAggregateMapStateData)stateData;

        state.AggregateIdsBySession.Add(
            ThreadId,
            MemoryAggregateId
        );

        return state;
    }
}
