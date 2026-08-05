using ActionModule.Shared.Models;
using EventSourcing.Core;
using PolicyModule.Application.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;

namespace PolicyModule.Application.Commands;

public sealed class AddGeneralPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Title));

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var policyId = PolicyId.New();

        await ExecuteGeneralPoliciesEvent(
            executor,
            new GeneralPolicyAddedV1(
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

public sealed class UpdateGeneralPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required Guid PolicyId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            PolicyId != Guid.Empty
            && !string.IsNullOrWhiteSpace(Title)
        );

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new GeneralPolicyUpdatedV1(
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

public sealed class RemoveGeneralPolicyCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(PolicyId != Guid.Empty);

    protected override Task<object> ExecuteInternal(
        Executor executor
    ) =>
        ExecuteGeneralPoliciesEvent(
            executor,
            new GeneralPolicyRemovedV1(
                PolicyModule.Domain.Models.PolicyId.FromDatabaseGuid(
                    PolicyId
                )
            )
        );
}
