using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Application.Queries;

public sealed class SearchProjectPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : ProjectQuery<PagedResult<PolicyDto>?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }
    public int Page { get; set; } = Pagination.DefaultPage;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && Pagination.IsValid(Page, PageSize)
        );

    protected override async Task<PagedResult<PolicyDto>?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(ProjectId);
        var state = await Replay<ProjectStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        if (state is null || state.IsDeleted)
            return null;

        return PolicySearch.Create(
            state.Policies.Values,
            Page,
            PageSize,
            Search
        );
    }
}
