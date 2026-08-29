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
    string PayloadJson
);

public interface IMemoryConversationRepository
{
    Task<List<MemoryConversationMessage>> Get(
        AggregateId memoryAggregateId,
        CancellationToken cancellationToken = default
    );

    Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemoryConversationMessage> messages,
        CancellationToken cancellationToken = default
    );
}
