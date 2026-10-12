using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureBranchDto(string Name, string Description)
{
    public static FeatureBranchDto FromModel(FeatureBranch branch) =>
        new(branch.Name, branch.Description);
}
