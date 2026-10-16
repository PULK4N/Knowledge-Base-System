namespace MemoryModule.Application.DTOs;

public sealed record MemoryConversationTextMessageDto(
    Guid PromptId,
    DateTime Timestamp,
    string Role,
    string Text
);
