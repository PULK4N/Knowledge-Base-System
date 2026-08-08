namespace MemoryModule.Persistence;

public sealed record MemoryEmbeddingOptions
{
    public const string SectionName = "MemoryEmbeddings";
    public const string DefaultModel = "qwen3-embedding:0.6b";
    public const int DefaultDimensions = 1024;

    public required Uri BaseUrl { get; init; }
    public string Model { get; init; } = DefaultModel;
    public int Dimensions { get; init; } = DefaultDimensions;
}
