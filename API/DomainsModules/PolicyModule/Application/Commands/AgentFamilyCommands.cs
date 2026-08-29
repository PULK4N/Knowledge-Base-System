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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyCreatedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Description
            )
        );
}

public sealed class UpdateAgentFamilyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(AgentFamilyName));

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyUpdatedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Description
            )
        );
}

public sealed class RemoveAgentFamilyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string AgentFamilyName { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(AgentFamilyName));

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyRemovedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                )
            )
        );
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
        var policyId = PolicyId.New();

        await ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyPolicyAddedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyPolicyUpdatedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                CreatePolicy(
                    Domain.Models.PolicyId.FromDatabaseGuid(PolicyId),
                    Title,
                    Description
                )
            )
        );
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

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new AgentFamilyPolicyRemovedV1(
                Domain.Models.AgentFamilyName.Normalized(
                    AgentFamilyName
                ),
                Domain.Models.PolicyId.FromDatabaseGuid(PolicyId)
            )
        );
}
