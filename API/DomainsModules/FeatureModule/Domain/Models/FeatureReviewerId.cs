using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeatureReviewerId(Guid Value)
{
    public static FeatureReviewerId New() => new(DatabaseFriendlyGuidGenerator.NewGuid());
    public static FeatureReviewerId FromDatabaseGuid(Guid value) => new(value);
}
