using ProjectModule.Shared;

namespace FeatureModule.Domain.Models;

public sealed class FeatureReviewer
{
    public FeatureReviewerId Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReviewLevel ReviewLevel { get; set; }
    /// <summary>Set on feature copies: the plan item this was copied from when its plan completed.</summary>
    public ItemOrigin? Origin { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
