namespace FeatureModule.Application.Models;

public sealed record FeatureReviewerCreatedCommandResult(string Status, Guid ReviewerId)
{
    public static FeatureReviewerCreatedCommandResult Ok(Guid reviewerId) => new("OK", reviewerId);
}
