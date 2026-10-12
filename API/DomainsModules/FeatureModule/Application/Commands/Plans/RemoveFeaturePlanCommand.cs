using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class RemoveFeaturePlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanRemovedV2(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
