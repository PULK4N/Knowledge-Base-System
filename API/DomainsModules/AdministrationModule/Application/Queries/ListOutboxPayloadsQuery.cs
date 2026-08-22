using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.Queries;

public sealed class ListOutboxPayloadsQuery(
    IOutboxAdministrationRepository repository
) : Query<PagedResult<OutboxPayloadDto>>
{
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public bool OnlyIncomplete { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(Pagination.IsValid(Page, PageSize));

    protected override async Task<PagedResult<OutboxPayloadDto>>
        ExecuteInternal(Executor executor)
    {
        var result = await repository.Search(
            Page,
            PageSize,
            OnlyIncomplete
        );

        return new PagedResult<OutboxPayloadDto>(
            result.Items
                .Select(OutboxPayloadDto.FromEntry)
                .ToList(),
            Page,
            PageSize,
            result.TotalCount
        );
    }
}
