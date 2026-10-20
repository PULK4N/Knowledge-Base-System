using ActionModule.Shared.Models;
using EventSourcing.Core;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Replaces the branches a base branch was created from. An empty list goes
/// back to the default, the previous plan's first base branch or the feature branch.
/// </summary>
public sealed class SetFeaturePlanBranchSourcesCommand(
    StateMachineHandler stateMachineHandler
) : FeaturePlanBranchCommand(stateMachineHandler)
{
    public List<FeaturePlanBranchSourceDto> Sources { get; set; } = [];

    protected override async Task<object> ExecuteInternal(
        Executor executor
    )
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteEvent(
            executor,
            new FeaturePlanBaseBranchSourcesSetV1(
                FeaturePlanId.FromDatabaseGuid(PlanId),
                BranchName.Trim(),
                [.. Sources.Select(source => source.ToModel())],
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
