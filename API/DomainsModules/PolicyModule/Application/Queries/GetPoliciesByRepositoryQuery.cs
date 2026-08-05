using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;
using PolicyModule.Persistence.Interfaces;
using SharedModule.Constants;
using SharedModule.Exceptions;

namespace PolicyModule.Application.Queries;

public sealed class GetPoliciesByRepositoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore,
    IPolicyTextRepository policyTextRepository
) : PolicyQuery<string>(stateCalculator, eventStore)
{
    public required string RepositoryPath { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(RepositoryPath)
        );

    protected override async Task<string> ExecuteInternal(
        Executor executor
    )
    {
        var repositoryMapAggregateId =
            AggregateId.FromDatabaseGuid(
                StateDataAggregateIds.RepositoryToProjectMap
            );
        var globalEvents = await GetEvents([repositoryMapAggregateId]);
        var repositoryMap = await Replay<RepositoryToProjectMapStateData>(
            globalEvents,
            repositoryMapAggregateId
        );

        if (
            repositoryMap is null
            || !repositoryMap.RepositoryToProjectMap.TryGetValue(
                RepositoryPath,
                out var projectAggregateId
            )
        )
            throw CreateNotFoundException();

        return await policyTextRepository.Get(projectAggregateId)
            ?? throw CreateNotFoundException();
    }

    private NotFoundException CreateNotFoundException() =>
        new(
            $"Policies for repository '{RepositoryPath}' were not found."
        );
}
