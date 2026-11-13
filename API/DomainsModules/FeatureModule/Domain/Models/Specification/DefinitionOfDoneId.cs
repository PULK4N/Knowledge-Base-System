using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct DefinitionOfDoneId(Guid Value)
{
    public static DefinitionOfDoneId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static DefinitionOfDoneId FromDatabaseGuid(Guid value) =>
        new(value);
}
