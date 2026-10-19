using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class SetCodexPromptUsageCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required PromptId PromptId { get; set; }
    public required long InputTokens { get; set; }
    public required long CachedInputTokens { get; set; }
    public required long CacheWriteInputTokens { get; set; }
    public required long OutputTokens { get; set; }
    public long? ReasoningOutputTokens { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ThreadId.Value != Guid.Empty && PromptId.Value != Guid.Empty);

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
                new CodexPromptUsageSetV1(
                    PromptId, InputTokens, CachedInputTokens, CacheWriteInputTokens, OutputTokens, ReasoningOutputTokens
                )
            )
        );

        return MemoryCommandResult.Ok;
    }
}
