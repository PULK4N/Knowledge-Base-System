using EventSourcing.Shared.Models;
using FeatureModule.Domain.Events;
using FeatureModule.Domain.Models;
using Shared.Interfaces;

namespace FeatureModule.Domain.Validators;

/// <summary>
/// Requirements need a known type and a description, and a new number is always
/// above the highest number ever used for its type, so removed numbers stay retired.
/// </summary>
public sealed class FeatureRequirementsValidator : IPreEventValidator
{
    private const string ItemName = "requirement";

    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (FeatureStateData)stateData;
        var error = payload.EventData switch
        {
            FeatureRequirementsAddedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? CheckAdded(spec, data.Requirements)
                : SpecificationEventScope.MissingPlan,
            FeatureRequirementsUpdatedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? CheckUpdated(spec, data.Requirements)
                : SpecificationEventScope.MissingPlan,
            FeatureRequirementsRemovedV1 data => state.FindSpecification(data.PlanId) is { } spec
                ? SpecificationEventScope.CheckIds( data.RequirementIds, spec.Requirements.Select(item => item.Id), true, ItemName)
                : SpecificationEventScope.MissingPlan,
            _ => $"{nameof(FeatureRequirementsValidator)} cannot validate {payload.EventData.GetType().Name}."
        };
        return EventValidationResult.FromPayload(payload, nameof(FeatureRequirementsValidator), error is null, error);
    }

    private static string? CheckAdded(Specification specification, IReadOnlyCollection<RequirementEntry> entries) =>
        SpecificationEventScope.CheckIds(entries.Select(entry => entry.Id).ToList(),
            specification.Requirements.Select(item => item.Id), false, ItemName)
        ?? CheckValues(entries)
        ?? CheckNewNumbers(specification, entries);

    private static string? CheckUpdated(Specification specification, IReadOnlyCollection<RequirementEntry> entries)
    {
        var error = SpecificationEventScope.CheckIds(entries.Select(entry => entry.Id).ToList(),
            specification.Requirements.Select(item => item.Id), true, ItemName) ?? CheckValues(entries);
        if (error is not null)
            return error;
        var current = specification.Requirements.ToDictionary(item => item.Id);
        if (entries.Any(entry => entry.Type == current[entry.Id].Type && entry.Number != current[entry.Id].Number))
            return "A requirement keeps its number unless its type changes.";
        return CheckNewNumbers(specification, entries.Where(entry => entry.Type != current[entry.Id].Type).ToList());
    }

    private static string? CheckValues(IReadOnlyCollection<RequirementEntry> entries) =>
        entries.Any(entry => !Enum.IsDefined(entry.Type)) ? "The requirement type is not known."
        : entries.Any(entry => string.IsNullOrWhiteSpace(entry.Description)) ? "A requirement description is required."
        : null;

    private static string? CheckNewNumbers(Specification specification, IReadOnlyCollection<RequirementEntry> entries) =>
        entries.Any(entry => entry.Number <= specification.LastRequirementNumber(entry.Type))
            ? "A requirement number was already used for its type."
            : entries.GroupBy(entry => (entry.Type, entry.Number)).Any(group => group.Count() > 1)
                ? "Two requirements of the same type have the same number."
                : null;
}
