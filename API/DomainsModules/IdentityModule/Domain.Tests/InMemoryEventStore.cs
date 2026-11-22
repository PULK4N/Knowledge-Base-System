using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;

namespace IdentityModule.Domain.Tests;

internal sealed class InMemoryEventStore(List<EventPayload> history) : IEventStoreWithOutbox
{
    public List<EventPayload> Written { get; } = [];

    public Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(List<AggregateId> aggregateIds) =>
        Task.FromResult(aggregateIds.ToDictionary(id => id, id => history.Concat(Written)
            .Where(payload => payload.EventExecutionInfo.AggregateId == id).ToList()));

    public Task Write(Dictionary<AggregateId, StateInfo> stateInfos)
    {
        Written.AddRange(stateInfos.Values.SelectMany(state => state.LastExecutedPayloads));
        return Task.CompletedTask;
    }
}
