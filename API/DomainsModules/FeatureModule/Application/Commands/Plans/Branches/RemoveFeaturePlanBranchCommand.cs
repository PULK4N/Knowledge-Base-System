using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class RemoveFeaturePlanBranchCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanBranchCommand(stateMachineHandler)
{
    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanBranchRemovedV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                BranchName.Trim(),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
