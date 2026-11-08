using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;
using SkillsModule.Domain.Events;

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
            {
                var project = (ProjectStateData)projectStateInfos[0].StateData;

                return project.RepositoryPaths
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
                    .Concat(
                        project.RelatedSkillIds.Select(
                            skillId => CreatePayload(
                                executor,
                                skillId,
                                Constants.StateMachineIds.Skills,
                                new SkillProjectRelationRemovedV1(
                                    projectAggregateId,
                                    SessionId,
                                    memoryAggregateId
                                )
                            )
                        )
                    )
                    .ToList();
            },
            memoryAggregateId
        );

        return PolicyCommandResult.Ok;
    }
}
