using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class SetClaudePromptUsageCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required PromptId PromptId { get; set; }
    public required long InputTokens { get; set; }
    public required long CacheReadInputTokens { get; set; }
    public required long CacheCreationInputTokens { get; set; }
    public required long OutputTokens { get; set; }
    public long? ThinkingTokens { get; set; }

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
                new ClaudePromptUsageSetV1(
                    PromptId, InputTokens, CacheReadInputTokens, CacheCreationInputTokens, OutputTokens, ThinkingTokens
                )
            )
        );

        return MemoryCommandResult.Ok;
    }
}
