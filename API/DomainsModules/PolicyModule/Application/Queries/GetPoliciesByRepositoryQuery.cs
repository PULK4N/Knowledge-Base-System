using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;
using PolicyModule.Application.Models;
using PolicyModule.Persistence.Interfaces;
using SharedModule.Constants;
using SharedModule.Exceptions;

namespace PolicyModule.Application.Queries;

public sealed class GetPoliciesByRepositoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore,
    IPolicyTextRepository policyTextRepository,
    IPolicyProjectSummaryRepository projectSummaryRepository
) : PolicyQuery<GetPoliciesByRepositoryResult>(stateCalculator, eventStore)
{
    public required string RepositoryPath { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(RepositoryPath)
        );

    protected override async Task<GetPoliciesByRepositoryResult> ExecuteInternal(
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
            return GetPoliciesByRepositoryResult.MappingRequired(
                RepositoryPath,
                (await projectSummaryRepository.List())
                    .Select(
                        project => new ProjectRepositoryOption(
                            project.ProjectId,
                            project.ProjectName,
                            project.RepositoryPaths
                        )
                    )
                    .ToList()
            );

        var policies = await policyTextRepository.Get(projectAggregateId)
            ?? throw await CreateNotFoundException();

        return GetPoliciesByRepositoryResult.Found(
            RepositoryPath,
            policies
        );
    }

    private async Task<NotFoundException> CreateNotFoundException() =>
        new($"""
            Policies for repository '{RepositoryPath}' were not found. 
            Create a project for this repository or connect the repository path to the one of the existing projects.
            Existing project names: {string.Join(", ", (await projectSummaryRepository.List()).Select(p => p.ProjectName))}
        """
        );
}
