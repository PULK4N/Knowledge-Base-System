using FeatureModule.Domain.Models;

namespace FeatureModule.Application.Commands;

/// <summary>Null values keep the current value.</summary>
public sealed record RequirementChangeInput(
    Guid Id,
    RequirementType? Type = null,
    string? Description = null,
    string? ExternalId = null,
    string? Source = null
);
