using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

public sealed record RequirementInput(
    RequirementType Type,
    string Description,
    string? ExternalId = null,
    string? Source = null
);
