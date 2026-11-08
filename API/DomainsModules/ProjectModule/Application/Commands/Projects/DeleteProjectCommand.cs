using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class DeleteProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        var projectAggregateId = AggregateId.FromDatabaseGuid(
            ProjectId
        );
        var projectDeleted = CreatePayload(
            executor,
            projectAggregateId,
            Constants.StateMachineIds.Project,
            new ProjectDeletedV2(
                SessionId,
                memoryAggregateId
            )
        );

        await ExecuteEvents(
            executor,
            projectDeleted,
            projectStateInfos =>
                ((ProjectStateData)projectStateInfos[0].StateData)
                    .RepositoryPaths
                    .Select(
                        repositoryPath => CreatePayload(
                            executor,
                            RepositoryToProjectMapAggregateId,
                            Constants.StateMachineIds.RepositoryToProjectMap,
                            new RepositoryToProjectMapRemovedV1(
                                repositoryPath,
                                projectAggregateId
                            )
                        )
                    )
                    .ToList(),
            memoryAggregateId
        );

        return PolicyCommandResult.Ok;
    }
}
