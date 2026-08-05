using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Models;

public readonly record struct PolicyId(Guid Value)
{
    public static PolicyId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static PolicyId FromDatabaseGuid(Guid guid) =>
        new(guid);
}
