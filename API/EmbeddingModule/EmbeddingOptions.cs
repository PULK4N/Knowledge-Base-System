namespace EmbeddingModule;

public sealed record EmbeddingOptions
{
    public const string SectionName = "Embeddings";
    public const string DefaultModel = "qwen3-embedding:0.6b";
    public const int DefaultDimensions = 1024;
    public const int ForceAllGpuLayers = 999;
    public const int DefaultMainGpu = 0;
    public const int DefaultNumCtx = 4096;

    public required Uri BaseUrl { get; init; }
    public string Model { get; init; } = DefaultModel;
    public int Dimensions { get; init; } = DefaultDimensions;
    public int NumGpu { get; init; } = ForceAllGpuLayers;
    public int MainGpu { get; init; } = DefaultMainGpu;
    public int NumCtx { get; init; } = DefaultNumCtx;
}
