using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class SetPromptModelCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required PromptId PromptId { get; set; }
    public required string Model { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Model));

    protected override async Task<MemoryCommandResult> ExecuteInternal(
        Executor executor
    )
    {
        var sessionMap = await stateMachineHandler.GetByAggregateId(
            MemoryAggregateIds.SessionAggregateMap
        );

        if (sessionMap?.StateData is not SessionAggregateMapStateData state
            || !state.AggregateIdsBySession.TryGetValue(
                ThreadId,
                out var memoryAggregateId
            ))
        {
            throw new InvalidOperationException(
                $"No memory exists for thread '{ThreadId.Value}'."
            );
        }

        await stateMachineHandler.ExecuteEvents(
            EventPayload.Create(
                executor.Id,
                memoryAggregateId,
                Constants.StateMachineIds.Memory,
                new PromptModelSetV1(PromptId, Model)
            )
        );

        return MemoryCommandResult.Ok;
    }
}
