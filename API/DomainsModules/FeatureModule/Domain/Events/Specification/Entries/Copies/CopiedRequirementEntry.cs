using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>A feature copy of a plan requirement: new ID and feature number, values at copy time.</summary>
public sealed record CopiedRequirementEntry(
    RequirementId Id,
    RequirementId OriginId,
    RequirementType Type,
    int Number,
    string Description,
    string ExternalId,
    string Source
);
