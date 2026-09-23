using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IMemoryRelationAdded : IEvent;

public readonly record struct MemoryRelationAddedV1(
    string Relation,
    AggregateId AggregateId
) : IMemoryRelationAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (MemoryStateData)stateData;
        state.Relations.Add(new MemoryRelation(Relation, AggregateId));
        return state;
    }
}
