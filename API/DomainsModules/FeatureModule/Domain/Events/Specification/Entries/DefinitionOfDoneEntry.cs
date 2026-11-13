using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>A new definition of done; Number is assigned by the command.</summary>
public sealed record DefinitionOfDoneEntry(
    DefinitionOfDoneId Id,
    int Number,
    string Description,
    ImmutableArray<RequirementId> RequirementIds
);
