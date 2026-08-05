using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Application.Queries;

public sealed class GetPoliciesByRepositoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : PolicyQuery<string?>(stateCalculator, eventStore)
{
    public required string RepositoryPath { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(RepositoryPath)
        );

    protected override async Task<string?> ExecuteInternal(
        Executor executor
    )
    {
        var generalPoliciesAggregateId =
            AggregateId.FromDatabaseGuid(
                StateDataAggregateIds.GeneralPolicies
            );
        var repositoryMapAggregateId =
            AggregateId.FromDatabaseGuid(
                StateDataAggregateIds.RepositoryToProjectMap
            );
        var globalEvents = await GetEvents(
            [generalPoliciesAggregateId, repositoryMapAggregateId]
        );
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
            return null;

        var projectEvents = await GetEvents(
            [projectAggregateId]
        );
        var projectPolicies = await Replay<ProjectPoliciesStateData>(
            projectEvents,
            projectAggregateId
        );

        if (projectPolicies is null || projectPolicies.IsDeleted)
            return null;

        var generalPolicies = await Replay<GeneralPoliciesStateData>(
            globalEvents,
            generalPoliciesAggregateId
        );

        return Compile(generalPolicies, projectPolicies);
    }

    private static string Compile(
        GeneralPoliciesStateData? generalPolicies,
        ProjectPoliciesStateData projectPolicies
    )
    {
        var policies = new List<Policy>();

        if (generalPolicies is not null)
            policies.AddRange(generalPolicies.Policies.Values);

        policies.AddRange(projectPolicies.Policies.Values);

        if (generalPolicies is not null)
        {
            foreach (var topicName in projectPolicies.RelatedTopics)
            {
                if (
                    generalPolicies.Topics.TryGetValue(
                        topicName,
                        out var topic
                    )
                )
                    policies.AddRange(topic.Policies.Values);
            }
        }

        return string.Join(
            "\n\n",
            policies.Select(
                policy => $"{policy.Title}\n{policy.Description}"
            )
        );
    }
}
