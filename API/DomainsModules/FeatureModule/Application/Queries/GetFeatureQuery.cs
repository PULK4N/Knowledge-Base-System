using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;

namespace FeatureModule.Application.Queries;

public sealed class GetFeatureQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : Query<FeatureDto?>
{
    public required Guid FeatureId { get; set; }

    /// <summary>
    /// Maximum event order to replay, or zero for the latest feature state.
    /// </summary>
    public uint OrderNumber { get; set; }

    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty);

    protected override async Task<FeatureDto?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(FeatureId);
        var eventsByAggregate = await eventStore.GetEvents([aggregateId]);

        if (!eventsByAggregate.TryGetValue(
                aggregateId,
                out var persistedEvents
            ))
            return null;

        var eventsToReplay = persistedEvents
            .Where(
                payload =>
                    OrderNumber == 0
                    || payload.EventExecutionInfo.OrderNumber <= OrderNumber
            )
            .OrderBy(payload => payload.EventExecutionInfo.OrderNumber)
            .ToList();

        if (eventsToReplay.Count == 0)
            return null;

        var stateInfo = await stateCalculator.Calculate(
            eventsToReplay,
            []
        );

        return FeatureDto.FromStateData(
            (FeatureStateData)stateInfo.StateData
        );
    }
}
