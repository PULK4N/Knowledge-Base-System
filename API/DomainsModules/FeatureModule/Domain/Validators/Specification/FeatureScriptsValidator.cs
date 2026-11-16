using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Scripts need a path and link only definitions of done of the same specification.
/// Paths do not have to be unique.
/// </summary>
public sealed class FeatureScriptsValidator : IPreEventValidator
{
    private const string ItemName = "Script";

    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (FeatureStateData)stateData;
        var error = payload.EventData switch
        {
            FeatureScriptsAddedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Check(spec, data.Scripts, false)
                : SpecificationEventScope.MissingPlan,
            FeatureScriptsUpdatedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Check(spec, data.Scripts, true)
                : SpecificationEventScope.MissingPlan,
            FeatureScriptsRemovedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.ScriptIds, true)
                : SpecificationEventScope.MissingPlan,
            FeatureScriptSnapshotsSetV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.Snapshots.Select(entry => entry.Id).ToList(), true)
                : SpecificationEventScope.MissingPlan,
            _ => $"{nameof(FeatureScriptsValidator)} cannot validate {payload.EventData.GetType().Name}."
        };
        return EventValidationResult.FromPayload(payload, nameof(FeatureScriptsValidator), error is null, error);
    }

    private static string? Check(Specification specification, IReadOnlyCollection<ScriptEntry> entries, bool mustExist) =>
        Ids(specification, entries.Select(entry => entry.Id).ToList(), mustExist)
        ?? (entries.Any(entry => string.IsNullOrWhiteSpace(entry.Path))
            ? "A script path is required."
            : SpecificationEventScope.CheckLinks(
                entries.Select(entry => (IReadOnlyCollection<DefinitionOfDoneId>)entry.DefinitionOfDoneIds),
                specification.DefinitionsOfDone.Select(item => item.Id), "definition of done"));

    private static string? Ids(Specification specification, IReadOnlyCollection<ScriptId> ids, bool mustExist) =>
        SpecificationEventScope.CheckIds(ids, specification.Scripts.Select(item => item.Id), mustExist, ItemName);
}
