using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// One branch of the feature's branch graph. FromKeys are the keys of the
/// branches it came from; a base branch can come from several.
/// </summary>
public sealed record FeatureBranchGraphNodeDto(
    string Key,
    string? Name,
    string Description,
    Guid? PlanId,
    string? PlanTitle,
    FeaturePlanStatus? PlanStatus,
    bool IsBaseBranch,
    IReadOnlyList<string> FromKeys
);
