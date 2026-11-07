using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Application.DTOs;
using ProjectModule.Domain;
using SharedModule.Constants;

namespace ProjectModule.Application.Queries;

public sealed class GetPolicyProjectByRepositoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : ProjectQuery<PolicyProjectDetailsDto?>(stateCalculator, eventStore)
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

        var project = await Replay<ProjectStateData>(
            await GetEvents([projectId]), projectId
        );
        return project is null || project.IsDeleted
            ? null
            : PolicyProjectDetailsDto.FromStateData(project);
    }
}
