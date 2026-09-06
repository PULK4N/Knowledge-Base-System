using Xunit;

namespace EmbeddingModule.Tests;

public sealed class EmbeddingTextChunkerTests
{
    [Fact]
    public void Split_starts_chunks_at_markdown_headings()
    {
        const string markdown =
            "Preamble\n\n# First\nFirst content\n\n## Second\nSecond content";

        var chunks = EmbeddingTextChunker.Split(markdown);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Preamble", chunks[0]);
        Assert.StartsWith("# First", chunks[1]);
        Assert.StartsWith("## Second", chunks[2]);
    }

    [Fact]
    public void Split_uses_overlapping_bounded_chunks_for_large_sections()
    {
        var markdown = $"# Large\n{new string('x', 4_000)}";

        var chunks = EmbeddingTextChunker.Split(markdown);

        Assert.True(chunks.Count > 1);
        Assert.All(
            chunks,
            chunk => Assert.InRange(
                chunk.Length,
                1,
                EmbeddingTextChunker.MaximumChunkLength
            )
        );
        Assert.Equal(
            chunks[0][^EmbeddingTextChunker.ChunkOverlapLength..],
            chunks[1][..EmbeddingTextChunker.ChunkOverlapLength]
        );
    }
}
