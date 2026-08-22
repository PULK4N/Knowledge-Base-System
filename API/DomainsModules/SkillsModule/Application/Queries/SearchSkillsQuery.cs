using ActionModule.Shared;
using ActionModule.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Queries;

public sealed class ListSkillsQuery(
    ISkillSummaryRepository skillSummaryRepository
) : Query<List<SkillSummaryDto>>
{
    protected override async Task<List<SkillSummaryDto>> ExecuteInternal(
        Executor executor
    ) =>
        (await skillSummaryRepository.List())
            .Select(SkillSummaryDto.FromReadModel)
            .ToList();
}

public sealed class GetSkillByNameQuery(
    ISkillSummaryRepository skillSummaryRepository
) : Query<SkillSummaryDto?>
{
    public required string Name { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override async Task<SkillSummaryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var skill = await skillSummaryRepository.GetByName(Name);

        return skill is null
            ? null
            : SkillSummaryDto.FromReadModel(skill);
    }
}

public sealed class SearchSkillsQuery(
    ISkillSummaryRepository skillSummaryRepository
) : Query<PagedResult<SkillSummaryDto>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<SkillSummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await skillSummaryRepository.Search(
            Page,
            PageSize,
            Search
        );

        return new PagedResult<SkillSummaryDto>(
            result.Items
                .Select(SkillSummaryDto.FromReadModel)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}
