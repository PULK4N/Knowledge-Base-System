namespace FeatureModule.Domain.Models;

/// <summary>
/// The source-control branch the feature is developed on. Plans without a
/// previous plan check out their base branch from it.
/// </summary>
public sealed class FeatureBranch
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
