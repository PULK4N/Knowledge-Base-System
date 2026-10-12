using ActionModule.Shared.Models;
using EventSourcing.Shared.Interfaces;
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
        var planId = FeaturePlanId.FromDatabaseGuid(PlanId);

        // Plans that followed the removed plan now branch from the feature branch.
        return await ExecuteEvents(
            executor,
            new FeaturePlanRemovedV2(planId, SessionId, memoryAggregateId),
            feature => feature.Plans
                .Where(plan => plan.PreviousPlanId == planId)
                .Select(plan => (IEvent)new FeaturePlanPreviousPlanSetV1(
                    plan.Id,
                    null,
                    SessionId,
                    memoryAggregateId
                ))
                .ToList(),
            memoryAggregateId
        );
    }
}
