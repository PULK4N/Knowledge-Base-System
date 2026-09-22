using EmbeddingModule;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence;

public sealed class SkillSearch(
    ITextEmbeddingGenerator embeddingGenerator,
    ISkillSearchRepository repository
) : ISkillSearch
{
    public async Task<IReadOnlyList<SkillSearchResult>> Search(
        string query,
        List<string> keywords,
        HybridSkillSearchOptions? options = null,
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

        return HybridSkillRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }

    public async Task<SkillSearchResults> SearchWithSources(
        string query,
        List<string> keywords,
        HybridSkillSearchOptions? options = null,
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

        return new SkillSearchResults(
            HybridSkillRanker.Fuse(
                textCandidates,
                vectorCandidates,
                options
            ),
            HybridSkillRanker.FuseSources(
                textSourceCandidates,
                vectorSourceCandidates,
                options
            )
        );
    }

    private static HybridSkillSearchOptions Prepare(
        string query,
        List<string> keywords,
        HybridSkillSearchOptions? options
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

        options ??= new HybridSkillSearchOptions();
        HybridSkillRanker.Validate(options);

        return options;
    }
}
