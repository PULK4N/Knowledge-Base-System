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
        HybridSkillSearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));

        options ??= new HybridSkillSearchOptions();
        HybridSkillRanker.Validate(options);

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

        return HybridSkillRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }
}
