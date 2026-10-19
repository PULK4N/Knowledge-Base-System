using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence.Interfaces;

/// <summary>
/// One recorded hook of a conversation, shaped for reading a whole chat in the
/// user interface rather than for retrieval: the conversation text is kept
/// separate from the raw payload so a reader can expand the transport details
/// of a single message.
/// </summary>
public sealed record MemoryConversationMessage(
    AggregateId MemoryAggregateId,
    ThreadId ThreadId,
    PromptId PromptId,
    int HookIndex,
    DateTime Timestamp,
    string HookEventName,
    string Role,
    string Message,
    string PayloadJson,
    long? InputTokens = null,
    long? CachedInputTokens = null,
    long? CacheWriteInputTokens = null,
    long? OutputTokens = null,
    long? ReasoningOutputTokens = null
);

/// <summary>
/// One message that carries chat text, without the raw hook payload.
/// </summary>
public sealed record MemoryConversationText(
    PromptId PromptId,
    int HookIndex,
    DateTime Timestamp,
    string Role,
    string Message,
    long? InputTokens = null,
    long? CachedInputTokens = null,
    long? CacheWriteInputTokens = null,
    long? OutputTokens = null,
    long? ReasoningOutputTokens = null
);

public sealed record MemoryConversationTextPage(
    List<MemoryConversationText> Messages,
    int TotalCount
);

public interface IMemoryConversationRepository
{
    Task<List<MemoryConversationMessage>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reads chat text in conversation order, skipping hooks without text.
    /// </summary>
    Task<MemoryConversationTextPage> GetText(
        AggregateId memoryAggregateId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryConversationMessage> messages,
        CancellationToken cancellationToken = default
    );
}
