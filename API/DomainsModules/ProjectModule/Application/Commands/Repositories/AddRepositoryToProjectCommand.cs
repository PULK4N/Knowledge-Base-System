using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class AddRepositoryToProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required string RepositoryPath { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(RepositoryPath)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        var projectAggregateId = AggregateId.FromDatabaseGuid(
            ProjectId
        );
        var repositoryAdded = CreatePayload(
            executor,
            projectAggregateId,
            Constants.StateMachineIds.Project,
            new RepositoryAddedToProjectV2(
                RepositoryPath,
                SessionId,
                memoryAggregateId
            )
        );

        await ExecuteEvents(
            executor,
            repositoryAdded,
            _ =>
                [
                    CreatePayload(
                        executor,
                        RepositoryToProjectMapAggregateId,
                        Constants.StateMachineIds.RepositoryToProjectMap,
                        new RepositoryToProjectMapAddedV1(
                            RepositoryPath,
                            projectAggregateId
                        )
                    )
                ],
            memoryAggregateId
        );

        return PolicyCommandResult.Ok;
    }
}
