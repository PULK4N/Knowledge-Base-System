using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record FeaturePlanBranchDto(
    string Name,
    string Description,
    IReadOnlyList<FeaturePlanBranchDto> SubBranches
)
{
    public static FeaturePlanBranchDto FromModel(FeaturePlanBranch branch) =>
        new(
            branch.Name,
            branch.Description,
            branch.SubBranches.Select(FromModel).ToList()
        );
}
