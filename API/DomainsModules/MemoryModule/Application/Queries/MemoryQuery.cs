using ActionModule.Shared;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;

namespace MemoryModule.Application.Queries;

public abstract class MemoryQuery<TResult>(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : Query<TResult>
{
    protected async Task<Dictionary<AggregateId, MemoryStateData>> GetStates(
        List<AggregateId> aggregateIds
    )
    {
        var distinctIds = aggregateIds.Distinct().ToList();
        var eventsByAggregate = await eventStore.GetEvents(distinctIds);
        var states = new Dictionary<AggregateId, MemoryStateData>();

        foreach (var aggregateId in distinctIds)
        {
            if (!eventsByAggregate.TryGetValue(
                    aggregateId,
                    out var persistedEvents
                )
                || persistedEvents.Count == 0)
            {
                continue;
            }

            var stateInfo = await stateCalculator.Calculate(
                persistedEvents
                    .OrderBy(
                        payload =>
                            payload.EventExecutionInfo.OrderNumber
                    )
                    .ToList(),
                []
            );

            if (stateInfo.StateData is MemoryStateData { IsDeleted: false } state)
                states.Add(aggregateId, state);
        }

        return states;
    }
}
