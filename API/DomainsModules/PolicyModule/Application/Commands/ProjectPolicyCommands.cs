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
        var projectId = AggregateId.New();
        var payloads = new List<EventPayload>
        {
            CreatePayload(
                executor,
                projectId,
                Constants.StateMachineIds.ProjectPolicies,
                new ProjectCreatedV1(
                    ProjectName,
                    ProjectDescription,
                    RepositoryPaths.ToImmutableArray()
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
        var policyId = PolicyId.New();

        await ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyAddedV1(
                CreatePolicy(
                    policyId,
                    Title,
                    Description
                )
            )
        );

        return PolicyAddedCommandResult.Ok(policyId.Value);
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            new ProjectUpdatedV1(
                ProjectName,
                ProjectDescription
            )
        );
}

public sealed class DeleteProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectPoliciesCommand(stateMachineHandler)
{
    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var projectAggregateId = AggregateId.FromDatabaseGuid(
            ProjectId
        );
        var projectDeleted = CreatePayload(
            executor,
            projectAggregateId,
            Constants.StateMachineIds.ProjectPolicies,
            new ProjectDeletedV1()
        );

        await ExecuteEvents(
            projectDeleted,
            projectStateInfo =>
                ((ProjectPoliciesStateData)projectStateInfo.StateData)
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
                    .ToList()
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyUpdatedV1(
                CreatePolicy(
                    PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                        PolicyId
                    ),
                    Title,
                    Description
                )
            )
        );
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            new ProjectPolicyRemovedV1(
                PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                    PolicyId
                )
            )
        );
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            new TopicRelationAddedToProjectV1(
                new TopicName(TopicName)
            )
        );
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            new TopicRelationRemovedFromProjectV1(
                new TopicName(TopicName)
            )
        );
}
