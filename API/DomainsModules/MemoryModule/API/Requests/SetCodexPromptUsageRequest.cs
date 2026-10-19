namespace MemoryModule.API.Requests;

public sealed record SetCodexPromptUsageRequest
{
    public required long InputTokens { get; init; }
    public required long CachedInputTokens { get; init; }
    public required long CacheWriteInputTokens { get; init; }
    public required long OutputTokens { get; init; }
    public long? ReasoningOutputTokens { get; init; }
}
