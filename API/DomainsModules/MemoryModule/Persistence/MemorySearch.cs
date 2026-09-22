using EmbeddingModule;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IMemorySearchRepository repository
) : IMemorySearch
{
    public async Task<IReadOnlyList<MemorySearchResult>> Search(
        string query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        )).Single();
        var textCandidates = await repository.SearchText(
            keywords,
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

    public async Task<MemorySearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        )).Single();
        var textCandidates = await repository.SearchText(
            keywords,
            options.CandidateCount,
            cancellationToken
        );
        var vectorCandidates = await repository.SearchVector(
            queryEmbedding,
            options.CandidateCount,
            cancellationToken
        );
        var textSourceCandidates = await repository.SearchTextBySource(
            keywords,
            options.SourceResultCount,
            options.SourceCandidateCount,
            cancellationToken
        );
        var vectorSourceCandidates = await repository.SearchVectorBySource(
            queryEmbedding,
            options.SourceResultCount,
            options.SourceCandidateCount,
            cancellationToken
        );

        return new MemorySearchResults(
            HybridMemoryRanker.Fuse(
                textCandidates,
                vectorCandidates,
                options
            ),
            HybridMemoryRanker.FuseSources(
                textSourceCandidates,
                vectorSourceCandidates,
                options
            )
        );
    }

    private static HybridMemorySearchOptions Prepare(
        string query,
        List<string> keywords,
        HybridMemorySearchOptions? options
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));

        if (!SearchKeywordLimits.AreValid(keywords))
        {
            throw new ArgumentException(
                $"Search keywords must contain between {SearchKeywordLimits.MinimumCount} and {SearchKeywordLimits.MaximumCount} non-empty words.",
                nameof(keywords)
            );
        }

        options ??= new HybridMemorySearchOptions();
        HybridMemoryRanker.Validate(options);

        return options;
    }
}
