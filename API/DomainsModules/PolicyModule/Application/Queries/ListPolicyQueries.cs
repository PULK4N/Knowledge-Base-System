using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Application.Queries;

public sealed class ListGeneralPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<List<PolicyDto>>(stateCalculator, eventStore)
{
    protected override async Task<List<PolicyDto>> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        return state?.Policies.Values
                .Select(PolicyDto.FromModel)
                .ToList()
            ?? [];
    }
}

public sealed class ListTopicPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<List<PolicyDto>?>(stateCalculator, eventStore)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override async Task<List<PolicyDto>?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
        var state = await Replay<GeneralPoliciesStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        if (
            state is null
            || !state.Topics.TryGetValue(
                new TopicName(TopicName),
                out var topic
            )
        )
            return null;

        return topic.Policies.Values
            .Select(PolicyDto.FromModel)
            .ToList();
    }
}

public sealed class ListProjectPoliciesQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<List<PolicyDto>?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected override async Task<List<PolicyDto>?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(ProjectId);
        var state = await Replay<ProjectPoliciesStateData>(
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
