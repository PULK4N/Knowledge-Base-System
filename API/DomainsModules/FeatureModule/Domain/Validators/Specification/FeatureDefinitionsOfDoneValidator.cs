using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Definitions of done need a description, link only requirements of the same
/// specification, and get numbers above the highest number ever used.
/// </summary>
public sealed class FeatureDefinitionsOfDoneValidator : IPreEventValidator
{
    private const string ItemName = "definition of done";

    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (FeatureStateData)stateData;
        var error = payload.EventData switch
        {
            FeatureDefinitionsOfDoneAddedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? CheckAdded(spec, data.DefinitionsOfDone)
                : SpecificationEventScope.MissingPlan,
            FeatureDefinitionsOfDoneUpdatedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? CheckUpdated(spec, data.DefinitionsOfDone)
                : SpecificationEventScope.MissingPlan,
            FeatureDefinitionsOfDoneRemovedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.DefinitionOfDoneIds, true)
                : SpecificationEventScope.MissingPlan,
            FeatureDefinitionsOfDoneStateSetV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? Ids(spec, data.States.Select(entry => entry.Id).ToList(), true)
                    ?? (data.States.Any(entry => !Enum.IsDefined(entry.State)) ? "The definition of done state is not known." : null)
                : SpecificationEventScope.MissingPlan,
            _ => $"{nameof(FeatureDefinitionsOfDoneValidator)} cannot validate {payload.EventData.GetType().Name}."
        };
        return EventValidationResult.FromPayload(payload, nameof(FeatureDefinitionsOfDoneValidator), error is null, error);
    }

    private static string? CheckAdded(Specification specification, IReadOnlyCollection<DefinitionOfDoneEntry> entries) =>
        Ids(specification, entries.Select(entry => entry.Id).ToList(), false)
        ?? Content(specification, entries.Select(entry => (entry.Description, (IReadOnlyCollection<RequirementId>)entry.RequirementIds)).ToList())
        ?? (entries.Any(entry => entry.Number <= specification.LastDefinitionOfDoneNumber)
            ? "A definition of done number was already used."
            : entries.Select(entry => entry.Number).Distinct().Count() != entries.Count
                ? "Two definitions of done have the same number."
                : null);

    private static string? CheckUpdated(Specification specification, IReadOnlyCollection<DefinitionOfDoneChange> changes) =>
        Ids(specification, changes.Select(change => change.Id).ToList(), true)
        ?? Content(specification, changes.Select(change => (change.Description, (IReadOnlyCollection<RequirementId>)change.RequirementIds)).ToList());

    private static string? Ids(Specification specification, IReadOnlyCollection<DefinitionOfDoneId> ids, bool mustExist) =>
        SpecificationEventScope.CheckIds(ids, specification.DefinitionsOfDone.Select(item => item.Id), mustExist, ItemName);

    private static string? Content(
        Specification specification,
        IReadOnlyCollection<(string Description, IReadOnlyCollection<RequirementId> RequirementIds)> items
    ) =>
        items.Any(item => string.IsNullOrWhiteSpace(item.Description))
            ? "A definition of done description is required."
            : SpecificationEventScope.CheckLinks(items.Select(item => item.RequirementIds),
                specification.Requirements.Select(requirement => requirement.Id), "requirement");
}
