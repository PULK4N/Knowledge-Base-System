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

    /// <summary>
    /// The plan done before this one, or null when the base branch comes from the feature branch.
    /// </summary>
    public Guid? PreviousPlanId { get; set; }

    public override async Task<bool> CanExecute(Executor executor) =>
        !string.IsNullOrWhiteSpace(BranchName) && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var planId = FeaturePlanId.New();

        await ExecuteEvents(
            executor,
            new FeaturePlanAddedV4(
                planId,
                Title,
                Content,
                ContentType,
                AdditionalInfo,
                Status,
                BranchName.Trim(),
                BranchDescription,
                PreviousPlanId is { } previousPlanId
                    ? FeaturePlanId.FromDatabaseGuid(previousPlanId)
                    : null,
                SessionId,
                memoryAggregateId
            ),
            _ => RecordIds is { Count: > 0 }
                ? CreateRecordLinkEvents(planId, memoryAggregateId)
                : [],
            memoryAggregateId
        );

        return FeaturePlanCreatedCommandResult.Ok(planId.Value);
    }
}
