using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct ScriptId(Guid Value)
{
    public static ScriptId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static ScriptId FromDatabaseGuid(Guid value) =>
        new(value);
}
