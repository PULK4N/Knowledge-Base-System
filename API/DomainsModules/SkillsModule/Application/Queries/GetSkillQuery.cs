using ActionModule;
using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Domain;

namespace SkillsModule.Application.Queries;

public sealed class GetSkillQuery(StateCalculator stateCalculator, IEventStore eventStore)
    : Query<SkillDto?>
{
    public required Guid SkillId { get; set; }
    public uint OrderNumber { get; set; }

    public override Task<bool> IsAuthorized(Executor executor) => Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(SkillId != Guid.Empty);

    protected override async Task<SkillDto?> ExecuteInternal(Executor executor)
    {
        var aggregateId = AggregateId.FromDatabaseGuid(SkillId);
        var eventsByAggregate = await eventStore.GetEvents([ aggregateId ]);

        if (!eventsByAggregate.TryGetValue(aggregateId, out var persistedEvents))
            return null;

        var eventsToReplay = persistedEvents
            .Where(
                payload => OrderNumber == 0 || payload.EventExecutionInfo.OrderNumber <= OrderNumber
            )
            .OrderBy(payload => payload.EventExecutionInfo.OrderNumber)
            .ToList();

        if (eventsToReplay.Count == 0)
            return null;

        var stateInfo = await stateCalculator.Calculate(eventsToReplay, [ ]);

        return SkillDto.FromStateData((SkillStateData)stateInfo.StateData);
    }
}
