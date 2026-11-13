using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>A requirement's values; Number is assigned by the command.</summary>
public sealed record RequirementEntry(
    RequirementId Id,
    RequirementType Type,
    int Number,
    string Description,
    string ExternalId,
    string Source
);
