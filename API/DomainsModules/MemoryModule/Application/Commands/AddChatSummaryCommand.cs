using System.Collections.Immutable;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Commands;

public sealed class AddChatSummaryCommand(
    StateMachineHandler stateMachineHandler
) : Command<MemoryCommandResult>
{
    public required ThreadId ThreadId { get; set; }
    public required string Summary { get; set; }

    public List<MemoryRelatedEntityDto> RelatedEntities { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(Summary)
            && RelatedEntities.All(entity =>
                entity is not null
                && Enum.IsDefined(entity.Type)
                && entity.Id != Guid.Empty)
        );

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

        var payload = EventPayload.Create(
            executor.Id,
            memoryAggregateId,
            Constants.StateMachineIds.Memory,
            new ChatSummaryAddedV2(
                Summary,
                RelatedEntities
                    .Select(entity => entity.ToDomain())
                    .ToImmutableHashSet()
            )
        );

        await stateMachineHandler.ExecuteEvents(payload);

        return MemoryCommandResult.Ok;
    }
}
