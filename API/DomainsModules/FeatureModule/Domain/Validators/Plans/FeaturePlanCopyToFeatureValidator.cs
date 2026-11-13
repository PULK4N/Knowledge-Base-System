using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Only a completed plan is copied, each plan item at most once, with new IDs,
/// feature numbers never used before and links to feature items only.
/// </summary>
public sealed class FeaturePlanCopyToFeatureValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var error = payload.EventData is FeaturePlanCopiedToFeatureV1 data
            ? Check((FeatureStateData)stateData, data)
            : $"{nameof(FeaturePlanCopyToFeatureValidator)} cannot validate {payload.EventData.GetType().Name}.";
        return EventValidationResult.FromPayload(payload, nameof(FeaturePlanCopyToFeatureValidator), error is null, error);
    }

    private static string? Check(FeatureStateData state, FeaturePlanCopiedToFeatureV1 data)
    {
        var plan = state.Plans.SingleOrDefault(item => item.Id == data.PlanId);
        if (plan is null)
            return SpecificationEventScope.MissingPlan;
        if (plan.Status != FeaturePlanStatus.Completed)
            return "Only a completed plan is copied to the feature.";

        var feature = state.Specification;
        var copiedOrigins = feature.Requirements.Select(item => item.Origin?.PlanItemId)
            .Concat(feature.DefinitionsOfDone.Select(item => item.Origin?.PlanItemId))
            .Concat(feature.PlaywrightScripts.Select(item => item.Origin?.PlanItemId))
            .Concat(state.ReviewNotes.Select(item => item.Origin?.PlanItemId))
            .Concat(state.Reviewers.Select(item => item.Origin?.PlanItemId))
            .OfType<Guid>()
            .ToHashSet();
        var origins = data.Requirements.Select(entry => entry.OriginId.Value)
            .Concat(data.DefinitionsOfDone.Select(entry => entry.OriginId.Value))
            .Concat(data.Scripts.Select(entry => entry.OriginId.Value))
            .Concat(data.ReviewNotes.Select(entry => entry.OriginId.Value))
            .Concat(data.Reviewers.Select(entry => entry.OriginId.Value))
            .ToList();
        if (origins.Distinct().Count() != origins.Count || origins.Any(copiedOrigins.Contains))
            return "A plan item is already copied to the feature.";

        var newIds = data.Requirements.Select(entry => entry.Id.Value)
            .Concat(data.DefinitionsOfDone.Select(entry => entry.Id.Value))
            .Concat(data.Scripts.Select(entry => entry.Id.Value))
            .Concat(data.ReviewNotes.Select(entry => entry.Id.Value))
            .Concat(data.Reviewers.Select(entry => entry.Id.Value))
            .ToList();
        var existingIds = feature.Requirements.Select(item => item.Id.Value)
            .Concat(feature.DefinitionsOfDone.Select(item => item.Id.Value))
            .Concat(feature.PlaywrightScripts.Select(item => item.Id.Value))
            .Concat(state.ReviewNotes.Select(item => item.Id.Value))
            .Concat(state.Reviewers.Select(item => item.Id.Value))
            .ToHashSet();
        if (newIds.Distinct().Count() != newIds.Count || newIds.Any(existingIds.Contains) || origins.Intersect(newIds).Any())
            return "A feature copy needs a new ID.";

        if (data.Requirements.Any(entry => entry.Number <= feature.LastRequirementNumber(entry.Type))
            || data.Requirements.GroupBy(entry => (entry.Type, entry.Number)).Any(group => group.Count() > 1)
            || data.DefinitionsOfDone.Any(entry => entry.Number <= feature.LastDefinitionOfDoneNumber)
            || data.DefinitionsOfDone.Select(entry => entry.Number).Distinct().Count() != data.DefinitionsOfDone.Length)
            return "A feature copy needs a number never used in the feature.";

        var requirementIds = feature.Requirements.Select(item => item.Id)
            .Concat(data.Requirements.Select(entry => entry.Id)).ToHashSet();
        var definitionOfDoneIds = feature.DefinitionsOfDone.Select(item => item.Id)
            .Concat(data.DefinitionsOfDone.Select(entry => entry.Id)).ToHashSet();
        if (data.DefinitionsOfDone.Any(entry => entry.RequirementIds.Any(id => !requirementIds.Contains(id)))
            || data.Scripts.Any(entry => entry.DefinitionOfDoneIds.Any(id => !definitionOfDoneIds.Contains(id))))
            return "Feature copies can link only feature items.";
        return null;
    }
}
