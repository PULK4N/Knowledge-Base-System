using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

/// <summary>
/// Reads a whole conversation for the user interface. It composes the two
/// read models the interface needs and, unlike the retrieval queries, applies
/// no token budget because the reader pages through the chat itself.
/// </summary>
public sealed class GetMemoryConversationQuery(
    IMemoryConversationRepository conversationRepository,
    IMemorySummaryRepository summaryRepository
) : Query<MemoryConversationDto?>
{
    public required Guid MemoryId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(MemoryId != Guid.Empty);

    protected override async Task<MemoryConversationDto?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(MemoryId);
        var messages = await conversationRepository.Get(aggregateId);
        var summary = await summaryRepository.Get(aggregateId);

        if (messages.Count == 0 && summary is null)
            return null;

        return new MemoryConversationDto(
            MemoryId,
            summary?.ThreadId.Value
                ?? messages.FirstOrDefault()?.ThreadId.Value
                ?? Guid.Empty,
            summary?.Summary ?? string.Empty,
            summary?.SummaryTimestamp,
            summary?.FirstPromptTimestamp,
            summary?.LastPromptTimestamp,
            messages.Select(ToDto).ToList()
        );
    }

    private static MemoryConversationMessageDto ToDto(
        MemoryConversationMessage message
    ) =>
        new(
            message.PromptId.Value,
            message.HookIndex,
            message.Timestamp,
            message.HookEventName,
            message.Role,
            message.Message,
            message.PayloadJson
        );
}
