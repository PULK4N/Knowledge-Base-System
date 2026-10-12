using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class SetFeaturePlanPreviousPlanCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    /// <summary>
    /// The plan done before this one, or null when the base branch comes from the feature branch.
    /// </summary>
    public Guid? PreviousPlanId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
            && PreviousPlanId != Guid.Empty
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanPreviousPlanSetV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                PreviousPlanId is { } previousPlanId
                    ? FeaturePlanId.FromDatabaseGuid(previousPlanId)
                    : null,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
