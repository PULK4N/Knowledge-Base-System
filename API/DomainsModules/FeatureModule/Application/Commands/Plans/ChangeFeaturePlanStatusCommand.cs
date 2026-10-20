using FeatureModule.Application.DTOs;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed class ChangeFeaturePlanStatusCommand(
    StateMachineHandler stateMachineHandler
) : ExistingFeatureCommand(stateMachineHandler)
{
    public required Guid PlanId { get; set; }

    public FeaturePlanStatus Status { get; set; }

    /// <summary>
    /// Only when completing: the branch each base branch was merged into. A
    /// base branch that is not listed was merged into its own sources.
    /// </summary>
    public List<FeaturePlanBranchMergeDto> MergedInto { get; set; } = [];

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            FeatureId != Guid.Empty
            && PlanId != Guid.Empty
            && Enum.IsDefined(Status)
            && (MergedInto.Count == 0 || Status == FeaturePlanStatus.Completed)
        );

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var planId = FeaturePlanId.FromDatabaseGuid(PlanId);

        return await ExecuteEvents(
            executor,
            new FeaturePlanStatusChangedV1(
                planId,
                Status,
                SessionId,
                memoryAggregateId
            ),
            feature => FeaturePlanCompletion.CreateEvents(
                feature,
                planId,
                MergedInto,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
