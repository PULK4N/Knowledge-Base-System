using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeatureRecordId(Guid Value)
{
    public static FeatureRecordId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeatureRecordId FromDatabaseGuid(Guid value) =>
        new(value);
}

public sealed class FeatureRecord
{
    public FeatureRecordId Id { get; init; }

    public string UserMessage { get; set; } = string.Empty;

    public string AiAnswer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
