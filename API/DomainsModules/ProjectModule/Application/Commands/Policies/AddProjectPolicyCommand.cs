using System.Collections.Immutable;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class AddProjectPolicyCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
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

        await ExecuteProjectEvent(
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
