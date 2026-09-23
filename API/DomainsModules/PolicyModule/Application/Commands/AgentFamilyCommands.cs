using ActionModule.Shared.Models;
using EventSourcing.Core;
using PolicyModule.Application.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;

namespace PolicyModule.Application.Commands;

public sealed class CreateAgentFamilyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(AgentFamilyName));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyCreatedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class UpdateAgentFamilyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(AgentFamilyName));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyUpdatedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class RemoveAgentFamilyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(AgentFamilyName));

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyRemovedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}

public sealed class AddAgentFamilyPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(AgentFamilyName)
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
            new AgentFamilyPolicyAddedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
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

public sealed class UpdateAgentFamilyPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required Guid PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(AgentFamilyName)
            && PolicyId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyPolicyUpdatedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                CreatePolicy(
                    Domain.Models.PolicyId.FromDatabaseGuid(PolicyId),
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

public sealed class RemoveAgentFamilyPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(AgentFamilyName)
            && PolicyId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyPolicyRemovedV2(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Domain.Models.PolicyId.FromDatabaseGuid(PolicyId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
