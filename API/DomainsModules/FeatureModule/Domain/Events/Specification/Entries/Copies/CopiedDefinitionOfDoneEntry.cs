using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>A feature copy of a plan definition of done; RequirementIds point to feature requirements.</summary>
public sealed record CopiedDefinitionOfDoneEntry(
    DefinitionOfDoneId Id,
    DefinitionOfDoneId OriginId,
    int Number,
    string Description,
    ImmutableArray<RequirementId> RequirementIds,
    DefinitionOfDoneState State,
    string StateDescription,
    DateTime? StateCheckedAt
);
