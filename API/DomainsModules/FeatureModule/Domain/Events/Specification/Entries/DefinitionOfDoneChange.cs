using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>New description and requirement links of a definition of done; its number does not change.</summary>
public sealed record DefinitionOfDoneChange(
    DefinitionOfDoneId Id,
    string Description,
    ImmutableArray<RequirementId> RequirementIds
);
