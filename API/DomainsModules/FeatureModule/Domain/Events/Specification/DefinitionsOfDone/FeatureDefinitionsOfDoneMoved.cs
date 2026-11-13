using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public interface IFeatureDefinitionsOfDoneMoved : IEvent;

/// <summary>
/// Moves definitions of done to another plan with every requirement they link
/// and every script that links them. Items keep their IDs, get new numbers in
/// the target plan and are removed from the source plan. Items left behind
/// lose their links to moved items.
/// </summary>
public sealed record FeatureDefinitionsOfDoneMovedV1(
    FeaturePlanId SourcePlanId,
    FeaturePlanId TargetPlanId,
    ImmutableArray<MovedSpecificationItemEntry<DefinitionOfDoneId>> DefinitionsOfDone,
    ImmutableArray<MovedSpecificationItemEntry<RequirementId>> Requirements,
    ImmutableArray<PlaywrightScriptId> ScriptIds,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IFeatureDefinitionsOfDoneMoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var state = (FeatureStateData)stateData;
        var source = state.SpecificationOf(SourcePlanId);
        var target = state.SpecificationOf(TargetPlanId);
        var definitionOfDoneIds = DefinitionsOfDone.Select(entry => entry.Id).ToHashSet();
        var requirementIds = Requirements.Select(entry => entry.Id).ToHashSet();
        var scriptIds = ScriptIds.ToHashSet();

        foreach (var entry in Requirements)
        {
            var requirement = source.Requirements.Single(item => item.Id == entry.Id);
            requirement.Number = entry.Number;
            target.Requirements.Add(requirement);
            target.UseRequirementNumber(requirement.Type, entry.Number);
        }
        foreach (var entry in DefinitionsOfDone)
        {
            var definitionOfDone = source.DefinitionsOfDone.Single(item => item.Id == entry.Id);
            definitionOfDone.Number = entry.Number;
            target.DefinitionsOfDone.Add(definitionOfDone);
            target.UseDefinitionOfDoneNumber(entry.Number);
        }
        foreach (var script in source.PlaywrightScripts.Where(item => scriptIds.Contains(item.Id)))
        {
            script.DefinitionOfDoneIds.RemoveAll(id => !definitionOfDoneIds.Contains(id));
            target.PlaywrightScripts.Add(script);
        }

        source.Requirements.RemoveAll(item => requirementIds.Contains(item.Id));
        source.DefinitionsOfDone.RemoveAll(item => definitionOfDoneIds.Contains(item.Id));
        source.PlaywrightScripts.RemoveAll(item => scriptIds.Contains(item.Id));
        foreach (var definitionOfDone in source.DefinitionsOfDone)
            definitionOfDone.RequirementIds.RemoveAll(requirementIds.Contains);

        SpecificationHistory.Record(state, eventExecutionInfo, MemoryAggregateId);
        return state;
    }
}
