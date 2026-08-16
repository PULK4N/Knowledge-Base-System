using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;
using EventSourcing.Core.Interfaces;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Commands;

public sealed class QueueProjectionReplayCommand(
    IStateMachineDefinitionProvider definitionProvider,
    IProjectionReplayRepository replayRepository,
    IOutbox outbox
) : Command<ProjectionReplayQueuedResult>
{
    public required string StateMachineId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(StateMachineId)
            && definitionProvider
                .GetAll()
                .Any(
                    definition =>
                        definition.Id == StateMachineId
                        && (
                            definition.Projections.Count > 0
                            || definition.Events.Values.Any(
                                eventDefinition =>
                                    eventDefinition.Projections.Count > 0
                            )
                        )
                )
        );

    protected override async Task<ProjectionReplayQueuedResult>
        ExecuteInternal(Executor executor)
    {
        var lastEvents = await replayRepository.GetLastEvents(
            StateMachineId
        );
        var stateInfos = lastEvents.ToDictionary(
            payload => payload.EventExecutionInfo.AggregateId,
            CreateStateInfo
        );

        if (stateInfos.Count > 0)
            await outbox.Write(stateInfos);

        return ProjectionReplayQueuedResult.Queued(
            stateInfos.Count
        );
    }

    private static StateInfo CreateStateInfo(EventPayload payload)
    {
        var executionInfo = payload.EventExecutionInfo;
        var stateInfo = StateInfo.Create(
            new object(),
            executionInfo.StateMachineId,
            executionInfo.AggregateId
        );
        stateInfo.CurrentOrderNumber = executionInfo.OrderNumber;
        stateInfo.LastUpdateTimestamp = executionInfo.Timestamp;
        stateInfo.LastExecutedPayloads = [payload];

        return stateInfo;
    }
}
