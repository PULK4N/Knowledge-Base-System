using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.Queries;

public sealed class ListOutboxPayloadsQuery(
    IOutboxAdministrationRepository repository
) : PagedQuery<OutboxPayloadDto>
{
    public bool OnlyIncomplete { get; set; }
    public string? State { get; set; }
    public Guid? AggregateId { get; set; }
    public OutboxPayloadSortField SortBy { get; set; } =
        OutboxPayloadSortField.Id;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Descending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && (State?.Length ?? 0) <= EntityQueryLimits.MaximumSearchLength
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<OutboxPayloadDto>>
        ExecuteInternal(Executor executor)
    {
        var result = await repository.Search(
            CreateEntityQuery(
                new OutboxPayloadSearchFilters(
                    OnlyIncomplete,
                    State,
                    AggregateId
                ),
                SortBy,
                SortDirection
            )
        );

        return result.Map(OutboxPayloadDto.FromEntry);
    }
}
