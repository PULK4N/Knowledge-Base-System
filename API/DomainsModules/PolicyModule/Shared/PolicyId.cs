using EventSourcing.Shared.Models;

namespace PolicyModule.Shared;

public readonly record struct PolicyId(Guid Value)
{
    public static PolicyId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static PolicyId FromDatabaseGuid(Guid guid) =>
        new(guid);
}
