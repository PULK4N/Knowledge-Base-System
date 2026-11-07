using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class CreateProjectCommand(
    StateMachineHandler stateMachineHandler
) : ProjectCommand(stateMachineHandler)
{
    public required string ProjectName { get; set; }
    public required string ProjectDescription { get; set; }
    public List<string> RepositoryPaths { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(ProjectName)
            && RepositoryPaths.All(
                path => !string.IsNullOrWhiteSpace(path)
            )
            && RepositoryPaths.Distinct(StringComparer.Ordinal).Count()
                == RepositoryPaths.Count
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        var projectId = AggregateId.New();
        var payloads = new List<EventPayload>
        {
            CreatePayload(
                executor,
                projectId,
                Constants.StateMachineIds.Project,
                new ProjectCreatedV2(
                    ProjectName,
                    ProjectDescription,
                    RepositoryPaths.ToImmutableArray(),
                    SessionId,
                    memoryAggregateId
                )
            )
        };

        payloads.AddRange(
            RepositoryPaths.Select(
                repositoryPath => CreatePayload(
                    executor,
                    RepositoryToProjectMapAggregateId,
                    Constants.StateMachineIds.RepositoryToProjectMap,
                    new RepositoryToProjectMapAddedV1(
                        repositoryPath,
                        projectId
                    )
                )
            )
        );

        AddMemoryRelation(payloads, executor, payloads[0], memoryAggregateId);
        await ExecuteEvents(payloads);

        return ProjectCreatedCommandResult.Ok(
            projectId.Value
        );
    }
}
