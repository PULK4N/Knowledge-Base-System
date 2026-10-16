namespace MemoryModule.Application.DTOs;

public sealed record MemoryConversationTextResult(
    Guid MemoryId,
    int Offset,
    int Limit,
    int TotalCount,
    bool HasMore,
    List<MemoryConversationTextMessageDto> Messages
);
