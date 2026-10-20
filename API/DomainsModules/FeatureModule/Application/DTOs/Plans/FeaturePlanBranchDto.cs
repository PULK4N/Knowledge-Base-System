using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <param name="Sources">
/// Only on base branches: where the branch came from. Empty means the default,
/// the previous plan's first base branch or the feature branch.
/// </param>
public sealed record FeaturePlanBranchDto(
    string Name,
    string Description,
    IReadOnlyList<FeaturePlanBranchDto> SubBranches,
    IReadOnlyList<FeaturePlanBranchSourceDto> Sources
)
{
    public static FeaturePlanBranchDto FromModel(FeaturePlanBranch branch) =>
        new(
            branch.Name,
            branch.Description,
            branch.SubBranches.Select(FromModel).ToList(),
            branch.Sources.Select(FeaturePlanBranchSourceDto.FromModel).ToList()
        );
}
