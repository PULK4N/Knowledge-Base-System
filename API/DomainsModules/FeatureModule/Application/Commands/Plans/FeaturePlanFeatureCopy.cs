using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>
/// Builds the copy of a completed plan's items to the feature. Items already
/// copied (found by their origin) are skipped and reused for link mapping, so
/// completing a reopened plan again copies only what is new.
/// </summary>
internal static class FeaturePlanFeatureCopy
{
    internal static FeaturePlanCopiedToFeatureV1? Create(
        FeatureStateData feature,
        FeaturePlan plan,
        Guid sessionId,
        AggregateId memoryAggregateId
    )
    {
        var target = feature.Specification;
        var source = plan.Specification;

        var requirementCopies = target.Requirements
            .Where(item => item.Origin is not null)
            .ToDictionary(item => RequirementId.FromDatabaseGuid(item.Origin!.PlanItemId), item => item.Id);
        var nextRequirementNumbers = new Dictionary<RequirementType, int>();
        var requirements = source.Requirements
            .Where(item => !requirementCopies.ContainsKey(item.Id))
            .OrderBy(item => item.Type).ThenBy(item => item.Number)
            .Select(item =>
            {
                var id = requirementCopies[item.Id] = RequirementId.New();
                var number = nextRequirementNumbers[item.Type] =
                    nextRequirementNumbers.GetValueOrDefault(item.Type, target.LastRequirementNumber(item.Type)) + 1;
                return new CopiedRequirementEntry(id, item.Id, item.Type, number, item.Description, item.ExternalId,
                    item.Source);
            })
            .ToList();

        var definitionOfDoneCopies = target.DefinitionsOfDone
            .Where(item => item.Origin is not null)
            .ToDictionary(item => DefinitionOfDoneId.FromDatabaseGuid(item.Origin!.PlanItemId), item => item.Id);
        var definitionsOfDone = source.DefinitionsOfDone
            .Where(item => !definitionOfDoneCopies.ContainsKey(item.Id))
            .OrderBy(item => item.Number)
            .Select((item, index) => new CopiedDefinitionOfDoneEntry(
                definitionOfDoneCopies[item.Id] = DefinitionOfDoneId.New(), item.Id,
                target.LastDefinitionOfDoneNumber + index + 1, item.Description,
                [.. item.RequirementIds.Where(requirementCopies.ContainsKey).Select(id => requirementCopies[id])],
                item.State, item.StateDescription, item.StateCheckedAt))
            .ToList();

        var copiedScripts = target.PlaywrightScripts.Select(item => item.Origin?.PlanItemId).OfType<Guid>().ToHashSet();
        var scripts = source.PlaywrightScripts
            .Where(item => !copiedScripts.Contains(item.Id.Value))
            .Select(item => new CopiedPlaywrightScriptEntry(PlaywrightScriptId.New(), item.Id, item.Path,
                [.. item.DefinitionOfDoneIds.Where(definitionOfDoneCopies.ContainsKey).Select(id => definitionOfDoneCopies[id])],
                item.Snapshot))
            .ToList();

        var copiedNotes = feature.ReviewNotes.Select(item => item.Origin?.PlanItemId).OfType<Guid>().ToHashSet();
        var reviewNotes = plan.ReviewNotes
            .Where(item => !copiedNotes.Contains(item.Id.Value))
            .Select(item => new CopiedReviewNoteEntry(FeatureReviewNoteId.New(), item.Id, item.Title, item.Content))
            .ToList();

        var copiedReviewers = feature.Reviewers.Select(item => item.Origin?.PlanItemId).OfType<Guid>().ToHashSet();
        var reviewers = plan.Reviewers
            .Where(item => !copiedReviewers.Contains(item.Id.Value))
            .Select(item => new CopiedReviewerEntry(FeatureReviewerId.New(), item.Id, item.Title, item.Description,
                item.ReviewLevel))
            .ToList();

        return requirements.Count + definitionsOfDone.Count + scripts.Count + reviewNotes.Count + reviewers.Count == 0
            ? null
            : new FeaturePlanCopiedToFeatureV1(plan.Id, [.. requirements], [.. definitionsOfDone], [.. scripts],
                [.. reviewNotes], [.. reviewers], sessionId, memoryAggregateId);
    }
}
