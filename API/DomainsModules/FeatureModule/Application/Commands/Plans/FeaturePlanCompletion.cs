using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Builds the follow-up events of a status change: once a plan is completed,
/// plans whose base branches came from its branches now come from where those
/// branches were merged. Nothing changes while the plan is not completed, and
/// running it again for a completed plan only updates plans added since.
/// </summary>
internal static class FeaturePlanCompletion
{
    internal static List<IEvent> CreateEvents(
        FeatureStateData feature,
        FeaturePlanId planId,
        IReadOnlyCollection<FeaturePlanBranchMergeDto> mergedInto,
        Guid sessionId,
        AggregateId memoryAggregateId
    )
    {
        var plan = feature.Plans.Single(item => item.Id == planId);
        if (plan.Status != FeaturePlanStatus.Completed)
            return [];

        return FeaturePlanMerge
            .RewriteFollowers(
                feature.Plans,
                planId,
                mergedInto.ToDictionary(
                    merge => merge.BranchName.Trim(),
                    merge => merge.ToSource()
                )
            )
            .Select(
                rewrite => (IEvent)new FeaturePlanBaseBranchSourcesSetV1(
                    rewrite.PlanId,
                    rewrite.BranchName,
                    [.. rewrite.Sources],
                    sessionId,
                    memoryAggregateId
                )
            )
            .ToList();
    }
}
