using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeaturePlanCopiedToFeature : IEvent;

/// <summary>
/// Copies a completed plan's requirements, definitions of done, Playwright
/// scripts, review notes and reviewers to the feature. Each copy has its own ID
/// and an origin pointing to the plan item; the plan keeps its items.
/// </summary>
public sealed record FeaturePlanCopiedToFeatureV1(
    FeaturePlanId PlanId,
    ImmutableArray<CopiedRequirementEntry> Requirements,
    ImmutableArray<CopiedDefinitionOfDoneEntry> DefinitionsOfDone,
    ImmutableArray<CopiedPlaywrightScriptEntry> Scripts,
    ImmutableArray<CopiedReviewNoteEntry> ReviewNotes,
    ImmutableArray<CopiedReviewerEntry> Reviewers,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeaturePlanCopiedToFeature
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var specification = state.Specification;
        ItemOrigin Origin(Guid planItemId) => new(PlanId, planItemId);

        foreach (var entry in Requirements)
        {
            specification.Requirements.Add(new Requirement
            {
                Id = entry.Id,
                Type = entry.Type,
                Number = entry.Number,
                Description = entry.Description,
                ExternalId = entry.ExternalId,
                Source = entry.Source,
                Origin = Origin(entry.OriginId.Value)
            });
            specification.UseRequirementNumber(entry.Type, entry.Number);
        }
        foreach (var entry in DefinitionsOfDone)
        {
            specification.DefinitionsOfDone.Add(new DefinitionOfDone
            {
                Id = entry.Id,
                Number = entry.Number,
                Description = entry.Description,
                RequirementIds = entry.RequirementIds.ToList(),
                State = entry.State,
                StateDescription = entry.StateDescription,
                StateCheckedAt = entry.StateCheckedAt,
                Origin = Origin(entry.OriginId.Value)
            });
            specification.UseDefinitionOfDoneNumber(entry.Number);
        }
        foreach (var entry in Scripts)
            specification.PlaywrightScripts.Add(new PlaywrightScript
            {
                Id = entry.Id,
                Path = entry.Path,
                DefinitionOfDoneIds = entry.DefinitionOfDoneIds.ToList(),
                Snapshot = entry.Snapshot,
                Origin = Origin(entry.OriginId.Value)
            });
        foreach (var entry in ReviewNotes)
            state.ReviewNotes.Add(new FeatureReviewNote
            {
                Id = entry.Id,
                Title = entry.Title,
                Content = entry.Content,
                Origin = Origin(entry.OriginId.Value),
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            });
        foreach (var entry in Reviewers)
            state.Reviewers.Add(new FeatureReviewer
            {
                Id = entry.Id,
                Title = entry.Title,
                Description = entry.Description,
                ReviewLevel = entry.ReviewLevel,
                Origin = Origin(entry.OriginId.Value),
                CreatedAt = eventExecutionInfo.Timestamp,
                UpdatedAt = eventExecutionInfo.Timestamp
            });

        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
