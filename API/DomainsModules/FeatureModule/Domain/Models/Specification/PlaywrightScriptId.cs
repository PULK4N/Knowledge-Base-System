using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct PlaywrightScriptId(Guid Value)
{
    public static PlaywrightScriptId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static PlaywrightScriptId FromDatabaseGuid(Guid value) =>
        new(value);
}
