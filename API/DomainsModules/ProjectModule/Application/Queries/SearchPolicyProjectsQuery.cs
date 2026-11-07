using ActionModule.Shared;
using ActionModule.Shared.Models;
using ProjectModule.Application.DTOs;
using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Application.Queries;

public sealed class SearchPolicyProjectsQuery(
    IPolicyProjectSummaryRepository repository
) : Query<PagedResult<PolicyProjectSummaryDto>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<PolicyProjectSummaryDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await repository.Search(Page, PageSize, Search);

        return new PagedResult<PolicyProjectSummaryDto>(
            result.Items
                .Select(PolicyProjectSummaryDto.FromReadModel)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}
