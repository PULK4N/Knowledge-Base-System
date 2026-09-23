using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using PolicyModule.Application.Models;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;

namespace PolicyModule.Application.Commands;

public sealed class CreateProjectCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
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
                Constants.StateMachineIds.ProjectPolicies,
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

public sealed class AddProjectPolicyCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        var policyId = PolicyId.New();

        await ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyAddedV2(
                CreatePolicy(
                    policyId,
                    Title,
                    Description
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );

        return PolicyAddedCommandResult.Ok(policyId.Value);
    }
}

public sealed class AddRepositoryToProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
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
            Constants.StateMachineIds.ProjectPolicies,
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

public sealed class UpdateProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required string ProjectName { get; set; }
    public required string ProjectDescription { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(ProjectName)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectPoliciesEvent(
            executor,
            new ProjectUpdatedV2(
                ProjectName,
                ProjectDescription,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class DeleteProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
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
            Constants.StateMachineIds.ProjectPolicies,
            new ProjectDeletedV2(
                SessionId,
                memoryAggregateId
            )
        );

        await ExecuteEvents(
            executor,
            projectDeleted,
            projectStateInfos =>
                ((ProjectPoliciesStateData)projectStateInfos[0].StateData)
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

public sealed class UpdateProjectPolicyCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required Guid PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && PolicyId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyUpdatedV2(
                CreatePolicy(
                    PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                        PolicyId
                    ),
                    Title,
                    Description
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class RemoveProjectPolicyCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && PolicyId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyRemovedV2(
                PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                    PolicyId
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class AddTopicRelationToProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(TopicName)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectPoliciesEvent(
            executor,
            new TopicRelationAddedToProjectV2(
                new TopicName(TopicName),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class RemoveTopicRelationFromProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            ProjectId != Guid.Empty
            && !string.IsNullOrWhiteSpace(TopicName)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectPoliciesEvent(
            executor,
            new TopicRelationRemovedFromProjectV2(
                new TopicName(TopicName),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
