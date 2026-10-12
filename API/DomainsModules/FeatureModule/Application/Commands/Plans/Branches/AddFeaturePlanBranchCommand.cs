using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class AddFeaturePlanBranchCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanBranchCommand(stateMachineHandler)
{
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Parent branch name. Null only adds a base branch to a plan created
    /// before plans required one.
    /// </summary>
    public string? ParentBranchName { get; set; }

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanBranchAddedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                BranchName.Trim(),
                Description,
                string.IsNullOrWhiteSpace(ParentBranchName)
                    ? null
                    : ParentBranchName.Trim(),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
