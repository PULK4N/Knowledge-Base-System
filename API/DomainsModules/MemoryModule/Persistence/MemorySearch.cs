using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySearch(
    IMemoryEmbeddingGenerator embeddingGenerator,
    IMemorySearchRepository repository
) : IMemorySearch
{
    public async Task<IReadOnlyList<MemorySearchResult>> Search(
        string query,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));

        options ??= new HybridMemorySearchOptions();
        HybridMemoryRanker.Validate(options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        )).Single();
        var textCandidates = await repository.SearchText(
            query,
            options.CandidateCount,
            cancellationToken
        );
        var vectorCandidates = await repository.SearchVector(
            queryEmbedding,
            options.CandidateCount,
            cancellationToken
        );

        return HybridMemoryRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }
}
