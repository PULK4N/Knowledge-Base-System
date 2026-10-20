using EmbeddingModule;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public sealed class FeatureRecordSearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IFeatureRecordSearchRepository repository
) : IFeatureRecordSearch
{
    public async Task<List<FeatureRecordSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridFeatureRecordSearchOptions? options = null,
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

        return HybridFeatureRecordRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }

    public async Task<FeatureRecordSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridFeatureRecordSearchOptions? options = null,
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

        return new FeatureRecordSearchResults(
            HybridFeatureRecordRanker.Fuse(
                textCandidates,
                vectorCandidates,
                options
            ),
            HybridFeatureRecordRanker.FuseSources(
                textSourceCandidates,
                vectorSourceCandidates,
                options
            )
        );
    }

    private static HybridFeatureRecordSearchOptions Prepare(
        string query,
        List<string> keywords,
        HybridFeatureRecordSearchOptions? options
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

        options ??= new HybridFeatureRecordSearchOptions();
        HybridFeatureRecordRanker.Validate(options);

        return options;
    }
}
