using EmbeddingModule;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public sealed class FeatureResearchSearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IFeatureResearchSearchRepository repository
) : IFeatureResearchSearch
{
    public async Task<List<FeatureResearchSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridFeatureResearchSearchOptions? options = null,
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

        return HybridFeatureResearchRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }

    public async Task<FeatureResearchSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridFeatureResearchSearchOptions? options = null,
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

        return new FeatureResearchSearchResults(
            HybridFeatureResearchRanker.Fuse(
                textCandidates,
                vectorCandidates,
                options
            ),
            HybridFeatureResearchRanker.FuseSources(
                textSourceCandidates,
                vectorSourceCandidates,
                options
            )
        );
    }

    private static HybridFeatureResearchSearchOptions Prepare(
        string query,
        List<string> keywords,
        HybridFeatureResearchSearchOptions? options
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

        options ??= new HybridFeatureResearchSearchOptions();
        HybridFeatureResearchRanker.Validate(options);

        return options;
    }
}
