using System.Collections.Immutable;
using EmbeddingModule;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySearch(
    ITextEmbeddingGenerator embeddingGenerator,
    IMemorySearchRepository repository
) : IMemorySearch
{
    private static readonly IReadOnlyList<MemorySearchCandidate> NoCandidates =
        [];

    public async Task<IReadOnlyList<MemorySearchResult>> Search(
        string? query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);
        var filter = options.Filter;

        var textCandidates = keywords.Count == 0
            ? NoCandidates
            : await repository.SearchText(
                keywords,
                options.CandidateCount,
                filter,
                cancellationToken
            );
        var vectorCandidates = await Embed(query, cancellationToken) is { } embedding
            ? await repository.SearchVector(
                embedding,
                options.CandidateCount,
                filter,
                cancellationToken
            )
            : NoCandidates;

        return HybridMemoryRanker.Fuse(
            textCandidates,
            vectorCandidates,
            options
        );
    }

    public async Task<MemorySearchResults> SearchWithSources(
        string? query,
        List<string> keywords,
        HybridMemorySearchOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        options = Prepare(query, keywords, options);
        var filter = options.Filter;

        var textCandidates = NoCandidates;
        var textSourceCandidates = NoCandidates;
        if (keywords.Count > 0)
        {
            textCandidates = await repository.SearchText(
                keywords,
                options.CandidateCount,
                filter,
                cancellationToken
            );
            textSourceCandidates = await repository.SearchTextBySource(
                keywords,
                options.SourceResultCount,
                options.SourceCandidateCount,
                filter,
                cancellationToken
            );
        }

        var vectorCandidates = NoCandidates;
        var vectorSourceCandidates = NoCandidates;
        if (await Embed(query, cancellationToken) is { } embedding)
        {
            vectorCandidates = await repository.SearchVector(
                embedding,
                options.CandidateCount,
                filter,
                cancellationToken
            );
            vectorSourceCandidates = await repository.SearchVectorBySource(
                embedding,
                options.SourceResultCount,
                options.SourceCandidateCount,
                filter,
                cancellationToken
            );
        }

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

    private async Task<ImmutableArray<float>?> Embed(
        string? query,
        CancellationToken cancellationToken
    ) =>
        string.IsNullOrWhiteSpace(query)
            ? null
            : (await embeddingGenerator.Generate(
                [query],
                cancellationToken
            )).Single();

    private static HybridMemorySearchOptions Prepare(
        string? query,
        List<string> keywords,
        HybridMemorySearchOptions? options
    )
    {
        if (string.IsNullOrWhiteSpace(query) && keywords.Count == 0)
        {
            throw new ArgumentException(
                "A search query or search keywords are required.",
                nameof(query)
            );
        }

        if (keywords.Count > 0 && !SearchKeywordLimits.AreValid(keywords))
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
