using ActionModule;
using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Domain;

namespace SkillsModule.Application.Queries;

public sealed class GetSkillQuery(
    StateMachineHandler stateMachineHandler,
    IEventStore eventStore
) : Query<SkillDto?>
{
    public required Guid SkillId { get; init; }
    public uint OrderNumber { get; init; }

    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(SkillId != Guid.Empty);

    protected override async Task<SkillDto?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = new AggregateId(SkillId);
        var eventsByAggregate = await eventStore.GetEvents(aggregateId);

        if (
            !eventsByAggregate.TryGetValue(
                aggregateId,
                out var persistedEvents
            )
        )
            return null;

        var eventsToReplay = persistedEvents
            .Where(
                payload =>
                    OrderNumber == 0
                    || payload.EventExecutionInfo.OrderNumber
                        <= OrderNumber
            )
            .OrderBy(
                payload => payload.EventExecutionInfo.OrderNumber
            )
            .ToList();

        if (eventsToReplay.Count == 0)
            return null;

        var stateInfo = await stateMachineHandler.Calculate(
            eventsToReplay,
            []
        );

        return SkillDto.FromStateData(
            (SkillStateData)stateInfo.StateData
        );
    }
}
