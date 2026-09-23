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

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicCreatedV2(
                new TopicName(TopicName),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class UpdateTopicCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicUpdatedV2(
                new TopicName(TopicName),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class RemoveTopicCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string TopicName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(TopicName));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicRemovedV2(
                new TopicName(TopicName),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
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
        var memoryAggregateId = await ResolveMemoryAggregateId();

        var policyId = PolicyId.New();

        await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyAddedV2(
                new TopicName(TopicName),
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

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyRemovedV2(
                new TopicName(TopicName),
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

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new TopicPolicyUpdatedV2(
                new TopicName(TopicName),
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
