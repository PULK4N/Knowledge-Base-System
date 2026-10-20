using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Sources must belong to an existing base branch and name the feature branch
/// or an existing branch of another plan, each once, without making branches
/// come from themselves.
/// </summary>
public sealed class FeaturePlanBranchSourcesValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not FeaturePlanBaseBranchSourcesSetV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(FeaturePlanBranchSourcesValidator),
                false,
                $"{nameof(FeaturePlanBranchSourcesValidator)} can only validate {nameof(FeaturePlanBaseBranchSourcesSetV1)} events."
            );
        }

        var plans = ((FeatureStateData)stateData).Plans;
        var plan = plans.SingleOrDefault(item => item.Id == eventData.PlanId);
        var sources = eventData.Sources.ToList();
        var error =
            plan?.FindBaseBranch(eventData.BranchName) is null
                ? $"The plan does not have a base branch named '{eventData.BranchName}'."
                : sources.Distinct().Count() != sources.Count
                    ? "A base branch cannot list the same source more than once."
                    : sources.FirstOrDefault(source => !Exists(source, plan.Id, plans)) is { } missing
                        ? $"The source branch '{missing.BranchName}' does not exist in another plan of this feature."
                        : FeatureBranchGraph.HasCycle(
                            FeatureBranchGraph.Build(
                                plans,
                                (candidatePlan, branch) =>
                                    candidatePlan.Id == plan.Id
                                    && branch.Name == eventData.BranchName
                                        ? sources
                                        : branch.Sources
                            )
                        )
                            ? "A branch cannot come from itself or from a branch created from it."
                            : null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(FeaturePlanBranchSourcesValidator),
            error is null,
            error
        );
    }

    private static bool Exists(
        FeaturePlanBranchSource source,
        FeaturePlanId planId,
        IReadOnlyCollection<FeaturePlan> plans
    ) =>
        source.PlanId is not { } sourcePlanId
        || (
            sourcePlanId != planId
            && plans.SingleOrDefault(plan => plan.Id == sourcePlanId)
                ?.FindBranch(source.BranchName) is not null
        );
}
