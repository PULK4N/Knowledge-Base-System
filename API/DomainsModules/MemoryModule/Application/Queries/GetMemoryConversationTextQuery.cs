using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Application.Queries;

/// <summary>
/// Reads the chat text of one memory in conversation order, a page of
/// messages at a time. Hooks without text and raw payloads are left out so a
/// model gets only what was said.
/// </summary>
public sealed class GetMemoryConversationTextQuery(
    IMemoryConversationRepository conversationRepository,
    IMemorySummaryRepository summaryRepository
) : Query<MemoryConversationTextResult>
{
    public const int DefaultLimit = 10;
    public const int MaximumLimit = Pagination.MaximumPageSize;

    public required Guid MemoryId { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = DefaultLimit;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            MemoryId != Guid.Empty
            && Offset is >= 0 and <= Pagination.MaximumOffset
            && Limit is >= 1 and <= MaximumLimit
        );

    protected override async Task<MemoryConversationTextResult> ExecuteInternal(
        Executor executor
    )
    {
        var page = await conversationRepository.GetText(
            AggregateId.FromDatabaseGuid(MemoryId),
            Offset,
            Limit
        );

        var summary = await summaryRepository.Get(
            AggregateId.FromDatabaseGuid(MemoryId)
        );

        return new MemoryConversationTextResult(
            MemoryId,
            Offset,
            Limit,
            page.TotalCount,
            Offset + page.Messages.Count < page.TotalCount,
            page.Messages
                .Select(
                    message => new MemoryConversationTextMessageDto(
                        message.PromptId.Value,
                        message.Timestamp,
                        message.Role,
                        message.Message,
                        message.InputTokens,
                        message.CachedInputTokens,
                        message.CacheWriteInputTokens,
                        message.OutputTokens,
                        message.ReasoningOutputTokens
                    )
                )
                .ToList(),
            summary?.InputTokens,
            summary?.CachedInputTokens,
            summary?.CacheWriteInputTokens,
            summary?.OutputTokens,
            summary?.ReasoningOutputTokens
        );
    }
}
