using ActionModule.Shared;
using ActionModule.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Queries;

public sealed class SearchSkillContentQuery(
    ISkillSearch skillSearch
) : Query<List<SkillSearchMatchDto>>
{
    public const int DefaultResultCount =
        HybridSkillSearchOptions.DefaultResultCount;
    public const int MinimumResultCount = 1;
    public const int MaximumResultCount = 20;

    private const int CandidateCount = 50;

    public required string SearchText { get; set; }
    public int ResultCount { get; set; } = DefaultResultCount;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(SearchText)
            && ResultCount is >= MinimumResultCount
                and <= MaximumResultCount
        );

    protected override async Task<List<SkillSearchMatchDto>> ExecuteInternal(
        Executor executor
    ) =>
        (await skillSearch.Search(
            SearchText,
            new HybridSkillSearchOptions
            {
                ResultCount = ResultCount,
                CandidateCount = CandidateCount
            }
        ))
            .Select(SkillSearchMatchDto.FromSearchResult)
            .ToList();
}
