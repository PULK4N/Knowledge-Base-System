namespace MemoryModule.API.Requests;

public sealed record SetClaudePromptUsageRequest
{
    public required long InputTokens { get; init; }
    public required long CacheReadInputTokens { get; init; }
    public required long CacheCreationInputTokens { get; init; }
    public required long OutputTokens { get; init; }
    public long? ThinkingTokens { get; init; }
}
