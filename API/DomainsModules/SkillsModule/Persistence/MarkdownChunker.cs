using EmbeddingModule;

namespace SkillsModule.Persistence;

public static class MarkdownChunker
{
    public const int MaximumChunkLength =
        EmbeddingTextChunker.MaximumChunkLength;
    public const int ChunkOverlapLength =
        EmbeddingTextChunker.ChunkOverlapLength;

    public static IReadOnlyList<string> Split(string markdown) =>
        EmbeddingTextChunker.Split(markdown);
}
