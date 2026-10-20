using FeatureModule.Application.DTOs;
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
    /// Parent branch name. Null adds another base branch to the plan.
    /// </summary>
    public string? ParentBranchName { get; set; }

    /// <summary>
    /// Only for a new base branch: the branches it was created from. Empty
    /// keeps the default, the previous plan's first base branch or the feature branch.
    /// </summary>
    public List<FeaturePlanBranchSourceDto> Sources { get; set; } = [];

    private bool AddsBaseBranch => string.IsNullOrWhiteSpace(ParentBranchName);

    public override async Task<bool> CanExecute(Executor executor) =>
        (AddsBaseBranch || Sources.Count == 0)
        && await base.CanExecute(executor);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var planId = FeaturePlanId.FromDatabaseGuid(PlanId);
        var branchName = BranchName.Trim();

        return await ExecuteEvents(
            executor,
            new FeaturePlanBranchAddedV1(
                planId,
                branchName,
                Description,
                string.IsNullOrWhiteSpace(ParentBranchName)
                    ? null
                    : ParentBranchName.Trim(),
                SessionId,
                memoryAggregateId
            ),
            _ => Sources.Count == 0
                ? []
                :
                [
                    new FeaturePlanBaseBranchSourcesSetV1(
                        planId,
                        branchName,
                        [.. Sources.Select(source => source.ToModel())],
                        SessionId,
                        memoryAggregateId
                    )
                ],
            memoryAggregateId
        );
    }
}
