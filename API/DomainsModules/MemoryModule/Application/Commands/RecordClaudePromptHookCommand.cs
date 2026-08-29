using System.Text.Json;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class RecordClaudePromptHookCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required PromptId PromptId { get; set; }
    public required string HookEventName { get; set; }
    public required JsonElement Payload { get; set; }

    protected override async Task<MemoryCommandResult> ExecuteInternal(
        Executor executor
    )
    {
        var sessionMap = await stateMachineHandler.GetByAggregateId(
            AggregateId.FromDatabaseGuid(
                SharedModule.Constants.StateDataAggregateIds.SessionAggregateMap
            )
        );

        if (sessionMap?.StateData is SessionAggregateMapStateData sessionAggregateMapStateData &&
                sessionAggregateMapStateData.AggregateIdsBySession.ContainsKey(ThreadId))
        {
            var aggregateId = sessionAggregateMapStateData.AggregateIdsBySession[ThreadId].Value;


            var payload = EventPayload.Create(
                executor.Id,
                AggregateId.FromDatabaseGuid(aggregateId),
                Constants.StateMachineIds.Memory,
                new ClaudePromptHookRecordedV1(
                    ThreadId,
                    PromptId,
                    HookEventName,
                    Payload
                )
            );

            await stateMachineHandler.ExecuteEvents(payload);

            return MemoryCommandResult.Ok;
        }


        var mapEvent = EventPayload.Create(
            executor.Id,
            MemoryAggregateIds.SessionAggregateMap,
            Constants.StateMachineIds.SessionAggregateMap,
            new SessionAggregateMapAddedV1(
                ThreadId,
                AggregateId.New()
            )
        );

        await stateMachineHandler.ExecuteEvents(
            mapEvent,
            stateInfo =>
            {
                var state = (SessionAggregateMapStateData)stateInfo.StateData;
                var memoryAggregateId = state.AggregateIdsBySession[ThreadId];

                return
                [
                    EventPayload.Create(
                        executor.Id,
                        memoryAggregateId,
                        Constants.StateMachineIds.Memory,
                        new ClaudePromptHookRecordedV1(
                            ThreadId,
                            PromptId,
                            HookEventName,
                            Payload
                        )
                    )
                ];
            }
        );

        return MemoryCommandResult.Ok;
    }
}
