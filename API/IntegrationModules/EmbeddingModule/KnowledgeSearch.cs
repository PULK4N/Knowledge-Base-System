namespace EmbeddingModule;

public sealed class KnowledgeSearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IKnowledgeSearchRepository repository
) : IKnowledgeSearch
{
    public async Task<List<KnowledgeSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        ))[0];
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

        return HybridKnowledgeSearchRanker.Rank(
            textCandidates,
            vectorCandidates,
            options
        );
    }

    public async Task<KnowledgeSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        ))[0];
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

        return new KnowledgeSearchResults(
            HybridKnowledgeSearchRanker.Rank(
                textCandidates,
                vectorCandidates,
                options
            ),
            HybridKnowledgeSearchRanker.RankBySource(
                textSourceCandidates,
                vectorSourceCandidates,
                options
            )
        );
    }

    private static HybridKnowledgeSearchOptions Prepare(
        string query,
        List<string> keywords,
        HybridKnowledgeSearchOptions? options
    )
    {
        if (string.IsNullOrWhiteSpace(query)
            || query.Length > KnowledgeSearchQueryLimits.MaximumLength)
        {
            throw new ArgumentException(
                $"Search query must contain between 1 and {KnowledgeSearchQueryLimits.MaximumLength} characters.",
                nameof(query)
            );
        }

        if (!SearchKeywordLimits.AreValid(keywords))
        {
            throw new ArgumentException(
                $"Search keywords must contain between {SearchKeywordLimits.MinimumCount} and {SearchKeywordLimits.MaximumCount} non-empty words.",
                nameof(keywords)
            );
        }

        options ??= new HybridKnowledgeSearchOptions();
        HybridKnowledgeSearchRanker.Validate(options);

        return options;
    }
}
