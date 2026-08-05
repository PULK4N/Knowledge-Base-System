using ActionModule.Shared;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Application.Queries;

public abstract class PolicyQuery<TResult>(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : Query<TResult>
{
    protected Task<Dictionary<AggregateId, List<EventPayload>>> GetEvents(
        List<AggregateId> aggregateIds
    ) =>
        eventStore.GetEvents(aggregateIds);

    protected async Task<TStateData?> Replay<TStateData>(
        Dictionary<AggregateId, List<EventPayload>> eventsByAggregate,
        AggregateId aggregateId
    ) where TStateData : class
    {
        if (
            !eventsByAggregate.TryGetValue(
                aggregateId,
                out var persistedEvents
            )
            || persistedEvents.Count == 0
        )
            return null;

        var stateInfo = await stateCalculator.Calculate(
            persistedEvents
                .OrderBy(
                    payload =>
                        payload.EventExecutionInfo.OrderNumber
                )
                .ToList(),
            []
        );

        return (TStateData)stateInfo.StateData;
    }
}
