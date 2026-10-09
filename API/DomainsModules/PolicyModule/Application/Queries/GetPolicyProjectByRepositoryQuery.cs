using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.DTOs;
using PolicyModule.Domain;
using SharedModule.Constants;

namespace PolicyModule.Application.Queries;

public sealed class GetPolicyProjectByRepositoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<PolicyProjectDetailsDto?>(stateCalculator, eventStore)
{
    public required string RepositoryPath { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(RepositoryPath));

    protected override async Task<PolicyProjectDetailsDto?> ExecuteInternal(
        Executor executor
    )
    {
        var mapId = AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
        var map = await Replay<RepositoryToProjectMapStateData>(
            await GetEvents([mapId]), mapId
        );
        if (map is null || !map.RepositoryToProjectMap.TryGetValue(
            RepositoryPath, out var projectId
        ))
            return null;

        var project = await Replay<ProjectPoliciesStateData>(
            await GetEvents([projectId]), projectId
        );
        return project is null || project.IsDeleted
            ? null
            : PolicyProjectDetailsDto.FromStateData(project);
    }
}
