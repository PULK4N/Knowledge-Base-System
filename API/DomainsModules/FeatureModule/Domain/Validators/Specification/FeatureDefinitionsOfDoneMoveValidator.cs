using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// A move goes to another, not completed plan of the feature and carries exactly
/// the requirements the moved definitions of done link and the scripts that link
/// them, each with a number never used in the target plan.
/// </summary>
public sealed class FeatureDefinitionsOfDoneMoveValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var error = payload.EventData is FeatureDefinitionsOfDoneMovedV1 data
            ? Check((FeatureStateData)stateData, data)
            : $"{nameof(FeatureDefinitionsOfDoneMoveValidator)} cannot validate {payload.EventData.GetType().Name}.";
        return EventValidationResult.FromPayload(payload, nameof(FeatureDefinitionsOfDoneMoveValidator), error is null, error);
    }

    private static string? Check(FeatureStateData state, FeatureDefinitionsOfDoneMovedV1 data)
    {
        var sourcePlan = state.Plans.SingleOrDefault(plan => plan.Id == data.SourcePlanId);
        var targetPlan = state.Plans.SingleOrDefault(plan => plan.Id == data.TargetPlanId);
        if (sourcePlan is null || targetPlan is null)
            return SpecificationEventScope.MissingPlan;
        if (sourcePlan.Id == targetPlan.Id)
            return "Definitions of done must move to a different plan.";
        if (targetPlan.Status == FeaturePlanStatus.Completed)
            return "Definitions of done cannot move to a completed plan.";

        var source = sourcePlan.Specification;
        var target = targetPlan.Specification;
        var definitionOfDoneIds = data.DefinitionsOfDone.Select(entry => entry.Id).ToList();
        var idError = SpecificationEventScope.CheckIds(definitionOfDoneIds,
            source.DefinitionsOfDone.Select(item => item.Id), true, "definition of done");
        if (idError is not null)
            return idError;

        var moved = definitionOfDoneIds.ToHashSet();
        var expectedRequirements = source.DefinitionsOfDone
            .Where(item => moved.Contains(item.Id))
            .SelectMany(item => item.RequirementIds)
            .ToHashSet();
        var expectedScripts = source.PlaywrightScripts
            .Where(item => item.DefinitionOfDoneIds.Any(moved.Contains))
            .Select(item => item.Id)
            .ToHashSet();
        if (!expectedRequirements.SetEquals(data.Requirements.Select(entry => entry.Id))
            || data.Requirements.Length != expectedRequirements.Count)
            return "A move must carry exactly the requirements the moved definitions of done link.";
        if (!expectedScripts.SetEquals(data.ScriptIds) || data.ScriptIds.Length != expectedScripts.Count)
            return "A move must carry exactly the Playwright scripts that link the moved definitions of done.";

        var requirementTypes = source.Requirements.ToDictionary(item => item.Id, item => item.Type);
        var requirementNumbers = data.Requirements
            .Select(entry => (Type: requirementTypes[entry.Id], entry.Number))
            .ToList();
        if (requirementNumbers.Any(item => item.Number <= target.LastRequirementNumber(item.Type))
            || requirementNumbers.Distinct().Count() != requirementNumbers.Count)
            return "A moved requirement needs a number never used for its type in the target plan.";
        if (data.DefinitionsOfDone.Any(entry => entry.Number <= target.LastDefinitionOfDoneNumber)
            || data.DefinitionsOfDone.Select(entry => entry.Number).Distinct().Count() != data.DefinitionsOfDone.Length)
            return "A moved definition of done needs a number never used in the target plan.";
        return null;
    }
}
