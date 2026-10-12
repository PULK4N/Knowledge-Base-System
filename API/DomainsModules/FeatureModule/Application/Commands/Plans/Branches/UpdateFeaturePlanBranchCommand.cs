using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class UpdateFeaturePlanBranchCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanBranchCommand(stateMachineHandler)
{
    /// <summary>
    /// New branch name, or null to keep the current name.
    /// </summary>
    public string? NewBranchName { get; set; }

    public string Description { get; set; } = string.Empty;

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var branchName = BranchName.Trim();

        return await ExecuteEvent(
            executor,
            new FeaturePlanBranchUpdatedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                branchName,
                string.IsNullOrWhiteSpace(NewBranchName)
                    ? branchName
                    : NewBranchName.Trim(),
                Description,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
