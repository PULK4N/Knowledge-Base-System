using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Scripts need a path and link only definitions of done of the same specification.
/// Paths do not have to be unique.
/// </summary>
public sealed class FeaturePlaywrightScriptsValidator : IPreEventValidator
{
    private const string ItemName = "Playwright script";

    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (FeatureStateData)stateData;
        var error = payload.EventData switch
        {
            FeaturePlaywrightScriptsAddedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Check(spec, data.Scripts, false)
                : SpecificationEventScope.MissingPlan,
            FeaturePlaywrightScriptsUpdatedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Check(spec, data.Scripts, true)
                : SpecificationEventScope.MissingPlan,
            FeaturePlaywrightScriptsRemovedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.ScriptIds, true)
                : SpecificationEventScope.MissingPlan,
            FeaturePlaywrightScriptSnapshotsSetV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.Snapshots.Select(entry => entry.Id).ToList(), true)
                : SpecificationEventScope.MissingPlan,
            _ => $"{nameof(FeaturePlaywrightScriptsValidator)} cannot validate {payload.EventData.GetType().Name}."
        };
        return EventValidationResult.FromPayload(payload, nameof(FeaturePlaywrightScriptsValidator), error is null, error);
    }

    private static string? Check(Specification specification, IReadOnlyCollection<PlaywrightScriptEntry> entries, bool mustExist) =>
        Ids(specification, entries.Select(entry => entry.Id).ToList(), mustExist)
        ?? (entries.Any(entry => string.IsNullOrWhiteSpace(entry.Path))
            ? "A Playwright script path is required."
            : SpecificationEventScope.CheckLinks(
                entries.Select(entry => (IReadOnlyCollection<DefinitionOfDoneId>)entry.DefinitionOfDoneIds),
                specification.DefinitionsOfDone.Select(item => item.Id), "definition of done"));

    private static string? Ids(Specification specification, IReadOnlyCollection<PlaywrightScriptId> ids, bool mustExist) =>
        SpecificationEventScope.CheckIds(ids, specification.PlaywrightScripts.Select(item => item.Id), mustExist, ItemName);
}
