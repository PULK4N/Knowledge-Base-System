namespace EmbeddingModule;

public sealed class KnowledgeSearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IKnowledgeSearchRepository repository
) : IKnowledgeSearch
{
    public async Task<List<KnowledgeSearchResult>> Search(
        string query,
        HybridKnowledgeSearchOptions? options = null,
        CancellationToken cancellationToken = default
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

        options ??= new HybridKnowledgeSearchOptions();
        HybridKnowledgeSearchRanker.Validate(options);

        var queryEmbedding = (await embeddingGenerator.Generate(
            [query],
            cancellationToken
        ))[0];
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

        return HybridKnowledgeSearchRanker.Rank(
            textCandidates,
            vectorCandidates,
            options
        );
    }
}
