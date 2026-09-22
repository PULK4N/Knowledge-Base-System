using ActionModule.Shared;
using ActionModule.Shared.Models;
using SkillsModule.Application.DTOs;
using EmbeddingModule;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Queries;

public sealed class SearchSkillContentQuery(
    ISkillSearch skillSearch
) : Query<SkillSearchResultsDto>
{
    public const int DefaultResultCount =
        HybridSkillSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 20;

    private const int CandidateCount = 50;

    public required string SearchText { get; set; }

    /// <summary>
    /// Words the full-text leg requires. The semantic leg reads SearchText.
    /// </summary>
    public List<string> Keywords { get; set; } = [];

    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && SearchKeywordLimits.AreValid(Keywords)
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<SkillSearchResultsDto> ExecuteInternal(
        Executor executor
    ) =>
        SkillSearchResultsDto.FromSearchResults(
            await skillSearch.SearchWithSources(
                SearchText,
                Keywords,
                new HybridSkillSearchOptions
                {
                    ResultCount = ResultCount,
                    CandidateCount = CandidateCount,
                    SourceResultCount = ResultCount
                }
            )
        );
}
