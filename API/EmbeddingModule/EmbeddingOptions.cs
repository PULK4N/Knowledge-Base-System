namespace EmbeddingModule;

public sealed record EmbeddingOptions
{
    public const string SectionName = "Embeddings";
    public const string DefaultModel = "qwen3-embedding:0.6b";
    public const int DefaultDimensions = 1024;

    public required Uri BaseUrl { get; init; }
    public string Model { get; init; } = DefaultModel;
    public int Dimensions { get; init; } = DefaultDimensions;
}
