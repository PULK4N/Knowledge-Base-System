using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanContentCommand(stateMachineHandler)
{
    public required string BranchName { get; set; }

    public string BranchDescription { get; set; } = string.Empty;

    public override async Task<bool> CanExecute(Executor executor) =>
        !string.IsNullOrWhiteSpace(BranchName) && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var planId = FeaturePlanId.New();

        await ExecuteEvent(
            executor,
            new FeaturePlanAddedV3(
                planId,
                Title,
                Content,
                ContentType,
                AdditionalInfo,
                Status,
                BranchName.Trim(),
                BranchDescription,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );

        return FeaturePlanCreatedCommandResult.Ok(planId.Value);
    }
}
