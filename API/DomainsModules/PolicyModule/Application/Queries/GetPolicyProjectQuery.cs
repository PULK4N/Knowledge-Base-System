using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Domain;

namespace PolicyModule.Application.Queries;

public sealed class GetPolicyProjectQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PolicyProjectDetailsDto?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected override async Task<PolicyProjectDetailsDto?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(ProjectId);
        var state = await Replay<ProjectPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        return state is null || state.IsDeleted
            ? null
            : PolicyProjectDetailsDto.FromStateData(state);
    }
}
