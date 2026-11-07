using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Application.Queries;

public sealed class ListProjectPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : ProjectQuery<List<PolicyDto>?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected override async Task<List<PolicyDto>?> ExecuteInternal(
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

        return state.Policies.Values
            .Select(PolicyDto.FromModel)
            .ToList();
    }
}
