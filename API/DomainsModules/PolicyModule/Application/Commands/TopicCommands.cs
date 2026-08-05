using ActionModule.Shared.Models;
using EventSourcing.Core;
using PolicyModule.Application.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;

namespace PolicyModule.Application.Commands;

public sealed class CreateTopicCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new TopicCreatedV1(
                new TopicName(TopicName),
                Description
            )
        );
}

public sealed class UpdateTopicCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new TopicUpdatedV1(
                new TopicName(TopicName),
                Description
            )
        );
}

public sealed class RemoveTopicCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new TopicRemovedV1(new TopicName(TopicName))
        );
}

public sealed class AddTopicPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(TopicName)
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var policyId = PolicyId.New();

        await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyAddedV1(
                new TopicName(TopicName),
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

public sealed class RemoveTopicPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(TopicName)
            && PolicyId != Guid.Empty
        );

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyRemovedV1(
                new TopicName(TopicName),
                PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                    PolicyId
                )
            )
        );
}

public sealed class UpdateTopicPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required Guid PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(TopicName)
            && PolicyId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyUpdatedV1(
                new TopicName(TopicName),
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
