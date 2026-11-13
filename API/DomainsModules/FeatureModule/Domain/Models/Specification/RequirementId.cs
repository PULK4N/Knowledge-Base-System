using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct RequirementId(Guid Value)
{
    public static RequirementId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static RequirementId FromDatabaseGuid(Guid value) =>
        new(value);
}
