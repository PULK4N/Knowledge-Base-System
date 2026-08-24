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
        HybridFeatureResearchSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));

        options ??= new HybridFeatureResearchSearchOptions();
        HybridFeatureResearchRanker.Validate(options);

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

        return HybridFeatureResearchRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }
}
