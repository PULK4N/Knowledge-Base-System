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

public sealed class MigrateCodexMemoryCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required string RawMemory { get; set; }
    public required string RolloutSummary { get; set; }
    public required string Source { get; set; }

    protected override async Task<MemoryCommandResult> ExecuteInternal(
        Executor executor
    )
    {
        var promptId = new PromptId(Guid.NewGuid());
        var payload = JsonSerializer.SerializeToElement(
            new
            {
                raw_memory = RawMemory,
                rollout_summary = RolloutSummary,
                source = Source
            }
        );
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
                        new CodexMemoryMigratedV1(
                            ThreadId,
                            promptId,
                            payload
                        )
                    )
                ];
            }
        );

        return MemoryCommandResult.Ok;
    }
}
